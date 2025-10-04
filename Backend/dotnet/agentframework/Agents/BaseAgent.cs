using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI;
using OpenAI.Chat;
using DotNetAgentFramework.Models;

namespace DotNetAgentFramework.Agents;

public interface IAgent
{
    string Name { get; }
    string Description { get; }
    string Instructions { get; }
    Task<string> RespondAsync(string message, string? context = null);
    Task<string> RespondAsync(string message, List<GroupChatMessage>? conversationHistory = null, string? context = null);
    Task<ChatResponse> ChatAsync(ChatRequest request);
    Task<ChatResponse> ChatWithHistoryAsync(ChatRequest request, List<GroupChatMessage>? conversationHistory = null);
    Task InitializeAsync();
}

public abstract class BaseAgent : IAgent
{
    protected readonly ILogger _logger;
    private ChatClient? _chatClient;

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Instructions { get; }

    protected BaseAgent(ILogger logger)
    {
        _logger = logger;
    }

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

            // Build messages for the chat completion
            var messages = new List<ChatMessage>
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

    public virtual async Task<ChatResponse> ChatAsync(ChatRequest request)
    {
        return await ChatWithHistoryAsync(request, null);
    }

    public virtual async Task<ChatResponse> ChatWithHistoryAsync(ChatRequest request, List<GroupChatMessage>? conversationHistory = null)
    {
        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;
        
        var content = await RespondAsync(request.Message, conversationHistory, request.Context);
        var endTime = DateTime.UtcNow;

        return new ChatResponse
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
public class AzureOpenAIAgent : BaseAgent
{
    private readonly string _modelDeployment;
    private readonly string _endpoint;
    private readonly Azure.Core.TokenCredential? _credential;

    public override string Name { get; }
    public override string Description { get; }
    public override string Instructions { get; }

    public AzureOpenAIAgent(
        string name,
        string description,
        string instructions,
        string modelDeployment,
        string endpoint,
        Azure.Core.TokenCredential? credential,
        ILogger<AzureOpenAIAgent> logger)
        : base(logger)
    {
        Name = name;
        Description = description;
        Instructions = instructions;
        _modelDeployment = modelDeployment;
        _endpoint = endpoint;
        _credential = credential ?? new DefaultAzureCredential();
    }

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
/// Azure AI Foundry Agent - Fallback implementation using Azure OpenAI
/// </summary>
public class AzureAIFoundryAgent : BaseAgent
{
    private readonly string _agentId;
    private readonly string _projectEndpoint;
    private readonly string? _managedIdentityClientId;
    private readonly string _modelDeployment;
    private readonly string _endpoint;
    private readonly Azure.Core.TokenCredential? _credential;

    public override string Name { get; }
    public override string Description { get; }
    public override string Instructions { get; }

    public AzureAIFoundryAgent(
        string name, 
        string agentId, 
        string projectEndpoint,
        string description,
        string instructions,
        string modelDeployment,
        string endpoint,
        Azure.Core.TokenCredential? credential,
        ILogger<AzureAIFoundryAgent> logger,
        string? managedIdentityClientId = null) 
        : base(logger)
    {
        Name = name;
        _agentId = agentId;
        _projectEndpoint = projectEndpoint;
        _managedIdentityClientId = managedIdentityClientId;
        Description = description;
        Instructions = instructions;
        _modelDeployment = modelDeployment;
        _endpoint = endpoint;
        _credential = credential ?? new DefaultAzureCredential();
    }

    public override async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing Azure AI Foundry agent {AgentId} for endpoint: {Endpoint}", _agentId, _projectEndpoint);

            // For now, use Azure OpenAI as the backend since the full Azure AI Foundry
            // integration with the new Agent Framework is still being developed
            var azureClient = new AzureOpenAIClient(new Uri(_endpoint), _credential);
            var chatClient = azureClient.GetChatClient(_modelDeployment);
            
            // Set the chat client for the base agent to use
            SetChatClient(chatClient);
            
            _logger.LogInformation("Initialized Azure AI Foundry agent {AgentName} with ID {AgentId}", Name, _agentId);
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
        // Add specific handling for Foundry agents if needed
        var enhancedContext = context;
        if (!string.IsNullOrEmpty(context))
        {
            enhancedContext += $"\n\nAgent ID: {_agentId}\nProject Endpoint: {_projectEndpoint}";
        }
        else
        {
            enhancedContext = $"Agent ID: {_agentId}\nProject Endpoint: {_projectEndpoint}";
        }

        return await base.RespondAsync(message, conversationHistory, enhancedContext);
    }
}