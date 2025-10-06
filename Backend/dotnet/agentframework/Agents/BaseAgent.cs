using Azure.AI.OpenAI;
using OpenAI.Chat;
using Azure.AI.Agents.Persistent;

namespace DotNetAgentFramework.Agents;

public interface IAgent
{
    string Name { get; }
    string Description { get; }
    string Instructions { get; }
    Task<string> RespondAsync(string message, string? context = null);
    Task<string> RespondAsync(string message, List<GroupChatMessage>? conversationHistory = null, string? context = null);
    Task<Models.ChatResponse> ChatAsync(ChatRequest request);
    Task<Models.ChatResponse> ChatWithHistoryAsync(ChatRequest request, List<GroupChatMessage>? conversationHistory = null);
    Task InitializeAsync();
}

public abstract class BaseAgent(ILogger logger) : IAgent
{
    protected readonly ILogger _logger = logger;
    protected ChatClient? _chatClient;

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Instructions { get; }

    public virtual async Task InitializeAsync()
    {
        try
        {
            // Initialize will be handled by derived classes
            _logger.LogDebug("Base initialization for agent {AgentName}", Name);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize agent {AgentName}", Name);
            throw;
        }
    }

    public virtual async Task<string> RespondAsync(string message, string? context = null)
    {
        return await RespondAsync(message, null, context);
    }

    public virtual async Task<string> RespondAsync(string message, List<GroupChatMessage>? conversationHistory = null, string? context = null)
    {
        if (_chatClient == null)
        {
            await InitializeAsync();
        }

        try
        {
            // Create instructions with context
            var systemPrompt = Instructions;
            if (!string.IsNullOrEmpty(context))
            {
                systemPrompt += $"\n\nAdditional Context: {context}";
            }

            // Build messages for the chat completion using OpenAI.Chat types
            var messages = new List<OpenAI.Chat.ChatMessage>
            {
                new SystemChatMessage(systemPrompt)
            };

            // Add conversation history
            if (conversationHistory != null && conversationHistory.Any())
            {
                foreach (var historyMessage in conversationHistory.OrderBy(m => m.Timestamp))
                {
                    if (historyMessage.Agent == "user")
                    {
                        messages.Add(new UserChatMessage(historyMessage.Content));
                    }
                    else
                    {
                        messages.Add(new AssistantChatMessage(historyMessage.Content));
                    }
                }
            }

            // Add current user message
            messages.Add(new UserChatMessage(message));

            // Get response from chat client
            if (_chatClient != null)
            {
                var response = await _chatClient.CompleteChatAsync(messages);
                return response.Value.Content[0].Text ?? "I apologize, but I couldn't generate a response.";
            }

            return "Agent not properly initialized.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {AgentName} responding to message", Name);
            return $"I encountered an error while processing your request: {ex.Message}";
        }
    }

    public virtual async Task<Models.ChatResponse> ChatAsync(ChatRequest request)
    {
        return await ChatWithHistoryAsync(request, null);
    }

    public virtual async Task<Models.ChatResponse> ChatWithHistoryAsync(ChatRequest request, List<GroupChatMessage>? conversationHistory = null)
    {
        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;
        
        var content = await RespondAsync(request.Message, conversationHistory, request.Context);
        var endTime = DateTime.UtcNow;

        return new Models.ChatResponse
        {
            Content = content,
            Agent = Name,
            SessionId = sessionId,
            Timestamp = endTime,
            Usage = new UsageInfo
            {
                PromptTokens = EstimateTokens(request.Message),
                CompletionTokens = EstimateTokens(content),
                TotalTokens = EstimateTokens(request.Message) + EstimateTokens(content)
            },
            ProcessingTimeMs = (int)(endTime - startTime).TotalMilliseconds
        };
    }

    protected virtual int EstimateTokens(string text)
    {
        // Simple token estimation (roughly 4 characters per token)
        return Math.Max(1, text.Length / 4);
    }

    protected void SetChatClient(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }
}

/// <summary>
/// Azure OpenAI Agent - Standard agent that uses Azure OpenAI
/// </summary>
public class AzureOpenAIAgent(
    string name,
    string description,
    string instructions,
    string modelDeployment,
    string endpoint,
    Azure.Core.TokenCredential? credential,
    ILogger<AzureOpenAIAgent> logger) : BaseAgent(logger)
{
    private readonly string _modelDeployment = modelDeployment;
    private readonly string _endpoint = endpoint;
    private readonly Azure.Core.TokenCredential? _credential = credential ?? new DefaultAzureCredential();

    public override string Name { get; } = name;
    public override string Description { get; } = description;
    public override string Instructions { get; } = instructions;

    public override async Task InitializeAsync()
    {
        try
        {
            // Create Azure OpenAI client and get chat client
            var azureClient = new AzureOpenAIClient(new Uri(_endpoint), _credential);
            var chatClient = azureClient.GetChatClient(_modelDeployment);
            
            // Set the chat client for the base agent to use
            SetChatClient(chatClient);
            
            _logger.LogInformation("Initialized Azure OpenAI agent {AgentName} with model {Model}", Name, _modelDeployment);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure OpenAI agent {AgentName}", Name);
            throw;
        }
    }
}

/// <summary>
/// Azure AI Foundry Agent using PersistentAgentsClient following the official sample pattern
/// </summary>
public class AzureAIFoundryAgent(
    string name,
    string agentId,
    string projectEndpoint,
    string description,
    string instructions,
    string modelDeployment,
    Azure.Core.TokenCredential? credential,
    ILogger<AzureAIFoundryAgent> logger,
    string? managedIdentityClientId = null) : BaseAgent(logger)
{
    private readonly string _agentId = agentId;
    private readonly string _projectEndpoint = projectEndpoint;
    private readonly string? _managedIdentityClientId = managedIdentityClientId;
    private readonly string _modelDeployment = modelDeployment;
    private readonly Azure.Core.TokenCredential? _credential = credential ?? new DefaultAzureCredential();
    private PersistentAgentsClient? _azureAgentClient;
    private Microsoft.Agents.AI.ChatClientAgent? _foundryAgent;
    private readonly Dictionary<string, Microsoft.Agents.AI.AgentThread> _threadCache = new();

    public override string Name { get; } = name;
    public override string Description { get; } = description;
    public override string Instructions { get; } = instructions;

    public override async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing Azure AI Foundry agent {AgentId} for endpoint: {Endpoint}", _agentId, _projectEndpoint);

            // Create PersistentAgentsClient following the sample pattern
            _azureAgentClient = new PersistentAgentsClient(_projectEndpoint, _credential);

            // Create or get the AI agent using the sample pattern
            _foundryAgent = await _azureAgentClient.GetAIAgentAsync(_agentId
                               );

            _logger.LogInformation("Initialized Azure AI Foundry agent {AgentName} with ID {AgentId}", Name, _foundryAgent.Id);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure AI Foundry agent {AgentName}", Name);
            throw;
        }
    }

    public override async Task<string> RespondAsync(string message, List<GroupChatMessage>? conversationHistory = null, string? context = null)
    {
        // Ensure agent is initialized
        if (_foundryAgent == null || _azureAgentClient == null)
        {
            await InitializeAsync();
        }

        if (_foundryAgent == null || _azureAgentClient == null)
        {
            throw new InvalidOperationException("Azure AI Foundry agent not properly initialized");
        }

        try
        {
            _logger.LogInformation("Processing message with Azure AI Foundry agent {AgentId}", _foundryAgent.Id);

            // Get or create thread for this conversation
            var thread = GetOrCreateThread(conversationHistory);

            // Create agent options following the sample pattern
            var agentOptions = new Microsoft.Agents.AI.ChatClientAgentRunOptions(new() { MaxOutputTokens = 1000 });

            // Add context to message if provided
            var enhancedMessage = message;
            if (!string.IsNullOrEmpty(context))
            {
                enhancedMessage = $"{message}\n\nAdditional Context: {context}";
            }

            // Run the agent following the sample pattern
            var result = await _foundryAgent.RunAsync(enhancedMessage, thread, agentOptions);
            
            // Extract content from the response - AgentRunResponse likely has a ToString() method or Response property
            var responseText = result?.ToString() ?? "I apologize, but I couldn't generate a response from the Azure AI Foundry agent.";
            
            _logger.LogInformation("Azure AI Foundry agent {AgentId} generated response: {ResponseLength} characters", 
                _foundryAgent.Id, responseText.Length);
            
            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing with Azure AI Foundry agent {AgentId}", _foundryAgent?.Id);
            throw;
        }
    }

    private Microsoft.Agents.AI.AgentThread GetOrCreateThread(List<GroupChatMessage>? conversationHistory)
    {
        // Simple thread management - in production you might want more sophisticated caching
        var threadKey = "default";
        if (conversationHistory != null && conversationHistory.Any())
        {
            // Create a thread key based on conversation history
            threadKey = $"conv_{conversationHistory.First().MessageId}";
        }

        if (!_threadCache.TryGetValue(threadKey, out var thread))
        {
            thread = _foundryAgent!.GetNewThread();
            _threadCache[threadKey] = thread;
            _logger.LogDebug("Created new thread for key: {ThreadKey}", threadKey);
        }

        return thread;
    }

    // Cleanup method to dispose of threads when agent is disposed
    public async ValueTask DisposeAsync()
    {
        if (_azureAgentClient != null && _foundryAgent != null)
        {
            // Clean up threads
            foreach (var thread in _threadCache.Values)
            {
                try
                {
                    if (thread is Microsoft.Agents.AI.ChatClientAgentThread chatThread)
                    {
                        await _azureAgentClient.Threads.DeleteThreadAsync(chatThread.ConversationId);
                        _logger.LogDebug("Cleaned up thread: {ConversationId}", chatThread.ConversationId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup thread for agent {AgentName}", Name);
                }
            }
            
            // Clean up agent
            //try
            //{
            //    await _azureAgentClient.Administration.DeleteAgentAsync(_foundryAgent.Id);
            //    _logger.LogDebug("Cleaned up agent: {AgentId}", _foundryAgent.Id);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogWarning(ex, "Failed to cleanup agent {AgentName}", Name);
            //}
        }
        
        _threadCache.Clear();
    }
}