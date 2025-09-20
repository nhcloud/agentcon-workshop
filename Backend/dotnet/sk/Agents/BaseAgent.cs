using DotNetSemanticKernel.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Azure.AI.Projects;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Azure.Core;
using System.Text;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace DotNetSemanticKernel.Agents;

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
    protected readonly Kernel _kernel;
    protected readonly ILogger _logger;
    internal ChatCompletionAgent? _chatAgent; // Made internal for GroupChatService access

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Instructions { get; }

    protected BaseAgent(Kernel kernel, ILogger logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public virtual async Task InitializeAsync()
    {
        try
        {
            // Initialize the ChatCompletionAgent following Python SK patterns
            _chatAgent = new ChatCompletionAgent()
            {
                Name = Name,
                Instructions = Instructions,
                Kernel = _kernel,
                Arguments = new KernelArguments()
            };
            
            _logger.LogDebug("Initialized ChatCompletionAgent for {AgentName}", Name);
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
        if (_chatAgent == null)
        {
            await InitializeAsync();
        }

        try
        {
            // Create chat history and include conversation history if provided
            var chatHistory = new ChatHistory();
            
            // Add system message with instructions
            var systemMessage = Instructions;
            if (!string.IsNullOrEmpty(context))
            {
                systemMessage += $"\n\nAdditional Context: {context}";
            }
            chatHistory.AddSystemMessage(systemMessage);
            
            // Add conversation history if provided
            if (conversationHistory != null && conversationHistory.Any())
            {
                foreach (var historyMessage in conversationHistory.OrderBy(m => m.Timestamp))
                {
                    if (historyMessage.Agent == "user")
                    {
                        chatHistory.AddUserMessage(historyMessage.Content);
                    }
                    else if (historyMessage.Agent == Name)
                    {
                        chatHistory.AddAssistantMessage(historyMessage.Content);
                    }
                    // Skip messages from other agents in group chats to avoid confusion
                }
            }
            
            // Add current user message
            chatHistory.AddUserMessage(message);

            // Get response from agent
            var responses = new List<ChatMessageContent>();
            await foreach (var content in _chatAgent!.InvokeAsync(chatHistory))
            {
                responses.Add(content);
            }

            var lastResponse = responses.LastOrDefault();
            return lastResponse?.Content ?? "I apologize, but I couldn't generate a response.";
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
}

/// <summary>
/// Azure OpenAI Agent - Standard agent that uses Azure OpenAI
/// </summary>
public class AzureOpenAIAgent : BaseAgent
{
    private readonly string _modelDeployment;

    public override string Name { get; }
    public override string Description { get; }
    public override string Instructions { get; }

    public AzureOpenAIAgent(
        string name,
        string description,
        string instructions,
        string modelDeployment,
        Kernel kernel,
        ILogger<AzureOpenAIAgent> logger)
        : base(kernel, logger)
    {
        Name = name;
        Description = description;
        Instructions = instructions;
        _modelDeployment = modelDeployment;
    }

    public override async Task InitializeAsync()
    {
        try
        {
            _chatAgent = new ChatCompletionAgent()
            {
                Name = Name,
                Instructions = $"{Instructions}\n\nPowered by Azure OpenAI ({_modelDeployment})",
                Kernel = _kernel,
                Arguments = new KernelArguments()
            };
            
            _logger.LogInformation("Initialized Azure OpenAI agent {AgentName} with model {Model}", Name, _modelDeployment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure OpenAI agent {AgentName}", Name);
            throw;
        }
    }
}

/// <summary>
/// Azure AI Foundry Agent that connects to pre-existing agents in Azure AI Foundry using Azure AI Projects SDK
/// This implementation uses a custom IChatCompletionService that wraps the Azure AI Foundry PersistentAgentsClient
/// </summary>
public class AzureAIFoundryAgent : BaseAgent
{
    private readonly string _agentId;
    private readonly string _projectEndpoint;
    private readonly string? _managedIdentityClientId;
    private AIProjectClient? _projectClient;
    private AzureAIFoundryService? _foundryService;

    public override string Name { get; }
    public override string Description { get; }
    public override string Instructions { get; }

    public AzureAIFoundryAgent(
        string name, 
        string agentId, 
        string projectEndpoint,
        string description,
        string instructions,
        Kernel kernel, 
        ILogger<AzureAIFoundryAgent> logger,
        string? managedIdentityClientId = null) 
        : base(kernel, logger)
    {
        Name = name;
        _agentId = agentId;
        _projectEndpoint = projectEndpoint;
        _managedIdentityClientId = managedIdentityClientId;
        Description = description;
        Instructions = instructions;
    }

    public override async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Creating Azure AI Foundry agent {AgentId} for endpoint: {Endpoint}", _agentId, _projectEndpoint);

            // Create the Azure AI Foundry wrapper service
            _foundryService = new AzureAIFoundryService(
                _projectEndpoint,
                _agentId,
                "ManagedIdentity",
                _logger,
                _managedIdentityClientId);

            await _foundryService.InitializeAsync();

            // Create a kernel with the custom Foundry chat completion service
            var builder = Kernel.CreateBuilder();
            builder.Services.AddSingleton<IChatCompletionService>(_foundryService);

            var foundryKernel = builder.Build();

            // Create a ChatCompletionAgent that uses our Foundry service
            _chatAgent = new ChatCompletionAgent()
            {
                Name = Name,
                Instructions = Instructions,
                Kernel = foundryKernel
            };
            
            _logger.LogInformation("Initialized Azure AI Foundry agent {AgentName} with ID {AgentId}", Name, _agentId);
            _logger.LogInformation("Azure AI Foundry project endpoint: {ProjectEndpoint}", _projectEndpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure AI Foundry agent {AgentName}", Name);
            throw;
        }
    }

    public override async Task<string> RespondAsync(string message, string? context = null)
    {
        return await RespondAsync(message, null, context);
    }

    public override async Task<string> RespondAsync(string message, List<GroupChatMessage>? conversationHistory = null, string? context = null)
    {
        if (_chatAgent == null)
        {
            await InitializeAsync();
        }

        try
        {
            // Set conversation context if available
            using var conversationContext = AzureAIFoundryService.BeginConversationContext(GetConversationId(conversationHistory));

            // Create chat history and include conversation history if provided
            var chatHistory = new ChatHistory();
            
            // Add system message with instructions
            var systemMessage = Instructions;
            if (!string.IsNullOrEmpty(context))
            {
                systemMessage += $"\n\nAdditional Context: {context}";
            }
            chatHistory.AddSystemMessage(systemMessage);
            
            // Add conversation history if provided
            if (conversationHistory != null && conversationHistory.Any())
            {
                foreach (var historyMessage in conversationHistory.OrderBy(m => m.Timestamp))
                {
                    if (historyMessage.Agent == "user")
                    {
                        chatHistory.AddUserMessage(historyMessage.Content);
                    }
                    else if (historyMessage.Agent == Name)
                    {
                        chatHistory.AddAssistantMessage(historyMessage.Content);
                    }
                    // Skip messages from other agents in group chats to avoid confusion
                }
            }
            
            // Add current user message
            chatHistory.AddUserMessage(message);

            // Get response from agent using the Azure AI Foundry service
            var responses = new List<ChatMessageContent>();
            await foreach (var content in _chatAgent!.InvokeAsync(chatHistory))
            {
                responses.Add(content);
            }

            var lastResponse = responses.LastOrDefault();
            var responseText = lastResponse?.Content ?? "I apologize, but I couldn't generate a response from the Azure AI Foundry agent.";
            
            _logger.LogInformation("Azure AI Foundry agent {AgentName} (ID: {AgentId}) generated response of length {Length}", 
                                 Name, _agentId, responseText.Length);
            
            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Azure AI Foundry agent {AgentName} responding to message", Name);
            return $"Azure AI Foundry agent error: {ex.Message}";
        }
    }

    private string? GetConversationId(List<GroupChatMessage>? conversationHistory)
    {
        // Generate a conversation ID based on the session or conversation context
        if (conversationHistory != null && conversationHistory.Any())
        {
            // Use a hash of the conversation to create a consistent ID
            var conversationContent = string.Join("|", conversationHistory.Take(3).Select(m => $"{m.Agent}:{m.Content.Substring(0, Math.Min(50, m.Content.Length))}"));
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(conversationContent)).Substring(0, 16);
        }
        return null;
    }

    public override async Task<ChatResponse> ChatAsync(ChatRequest request)
    {
        return await ChatWithHistoryAsync(request, null);
    }

    public override async Task<ChatResponse> ChatWithHistoryAsync(ChatRequest request, List<GroupChatMessage>? conversationHistory = null)
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
}

/// <summary>
/// Azure AI Foundry chat completion service that implements IChatCompletionService
/// </summary>
public class AzureAIFoundryService : IChatCompletionService
{
    private readonly string _endpoint;
    private readonly string _agentId;
    private readonly string _credentialType;
    private readonly string? _managedIdentityClientId;
    private readonly ILogger _logger;
    private AIProjectClient? _projectClient;

    // Maintain a per-conversation thread map so the same PersistentAgentThread is reused for a chat session
    // Make it static to survive different service instances (e.g., different plugin scopes / agent cache keys)
    private static readonly ConcurrentDictionary<string, string> s_threadByAgentConversation = new(StringComparer.Ordinal);

    // Ambient conversation context so callers (AgentManager) can indicate current conversation id
    private static readonly AsyncLocal<string?> s_currentConversationId = new();

    public AzureAIFoundryService(string endpoint, string agentId, string credentialType, ILogger logger, string? managedIdentityClientId = null)
    {
        _endpoint = endpoint;
        _agentId = agentId;
        _credentialType = credentialType;
        _managedIdentityClientId = managedIdentityClientId;
        _logger = logger;
    }

    public static IDisposable BeginConversationContext(string? conversationId)
    {
        var previous = s_currentConversationId.Value;
        s_currentConversationId.Value = conversationId;
        return new Popper(() => s_currentConversationId.Value = previous);
    }

    private static string? GetCurrentConversationId() => s_currentConversationId.Value;

    private sealed class Popper(Action pop) : IDisposable
    {
        public void Dispose() => pop();
    }

    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing Azure AI Foundry Service for agent {AgentId} with endpoint {Endpoint}",
                _agentId, _endpoint);

            TokenCredential credential;
            
            if (!string.IsNullOrEmpty(_managedIdentityClientId))
            {
                _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", _managedIdentityClientId);
                credential = new DefaultAzureCredential(
                    new DefaultAzureCredentialOptions
                    {
                        ManagedIdentityClientId = _managedIdentityClientId
                    });
            }
            else
            {
                _logger.LogInformation("Using DefaultAzureCredential without specific client ID");
                credential = new DefaultAzureCredential();
            }
            
            _projectClient = new AIProjectClient(new Uri(_endpoint), credential);

            _logger.LogInformation("Successfully initialized Azure AI Foundry client for agent {AgentId}", _agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure AI Foundry client for agent {AgentId}", _agentId);
            throw;
        }
    }

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AzureAIFoundryService.GetChatMessageContentsAsync called for agent {AgentId}", _agentId);

        if (_projectClient == null)
        {
            throw new InvalidOperationException("Service not initialized. Call InitializeAsync first.");
        }

        try
        {
            _logger.LogInformation("Processing message with Azure AI Foundry agent {AgentId}", _agentId);

            var userMessage = chatHistory.LastOrDefault(m => m.Role == AuthorRole.User)?.Content ?? string.Empty;
            _logger.LogInformation("User message for agent {AgentId}: {Message}", _agentId, userMessage);

            // Get the persistent agents client
            var agentsClient = _projectClient.GetPersistentAgentsClient();

            // Get the agent
            var agent = agentsClient.Administration.GetAgent(_agentId, cancellationToken);
            _logger.LogInformation("Retrieved Azure AI Foundry agent {AgentId}: {AgentName}", _agentId, agent.Value.Name);

            // Determine thread for the current conversation
            string? conversationId = GetCurrentConversationId();
            string threadId;
            bool isNewThread = false;
            
            if (!string.IsNullOrWhiteSpace(conversationId))
            {
                var key = $"{_endpoint}::{_agentId}::{conversationId}";
                if (!s_threadByAgentConversation.TryGetValue(key, out threadId!))
                {
                    var threadResponse = agentsClient.Threads.CreateThread();
                    threadId = threadResponse.Value.Id;
                    s_threadByAgentConversation[key] = threadId;
                    isNewThread = true;
                    _logger.LogDebug("Created new thread {ThreadId} for conversation {ConversationId} (key: {Key})", threadId, conversationId, key);
                }
                else
                {
                    _logger.LogDebug("Reusing existing thread {ThreadId} for conversation {ConversationId} (key: {Key})", threadId, conversationId, key);
                }
            }
            else
            {
                // Fallback: no conversation id provided, create a new thread each time
                var threadResponse = agentsClient.Threads.CreateThread();
                threadId = threadResponse.Value.Id;
                isNewThread = true;
                _logger.LogDebug("Created thread {ThreadId} without conversation context", threadId);
            }

            // If this is a new thread, add all chat history messages to establish context
            if (isNewThread && chatHistory.Count > 1)
            {
                _logger.LogDebug("Adding {Count} chat history messages to new thread {ThreadId}", chatHistory.Count, threadId);
                
                // Add all messages except the last one (which we'll add separately)
                foreach (var historyMessage in chatHistory.Take(chatHistory.Count - 1))
                {
                    // Only add User and Assistant messages (skip System messages as they're handled differently in Azure AI Foundry)
                    if (historyMessage.Role == AuthorRole.User)
                    {
                        var content = historyMessage.Content ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            try
                            {
                                agentsClient.Messages.CreateMessage(
                                    threadId,
                                    MessageRole.User,
                                    content,
                                    cancellationToken: cancellationToken);
                                
                                _logger.LogDebug("Added User message to thread {ThreadId}: {Content}", 
                                    threadId, content.Substring(0, Math.Min(100, content.Length)));
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to add User message to thread {ThreadId}", threadId);
                            }
                        }
                    }
                    // Note: For assistant messages, we can't directly add them to the thread as they come from the agent itself
                    // The thread will maintain its own conversation history through runs
                }
            }

            // Add the current user message to the thread
            var messageResponse = agentsClient.Messages.CreateMessage(
                threadId,
                MessageRole.User,
                userMessage,
                cancellationToken: cancellationToken);

            // Create and execute the run
            var run = agentsClient.Runs.CreateRun(threadId, agent.Value.Id, cancellationToken: cancellationToken);
            _logger.LogInformation("Created run {RunId} for thread {ThreadId}", run.Value.Id, threadId);

            // Poll until completion
            do
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                run = agentsClient.Runs.GetRun(threadId, run.Value.Id, cancellationToken);
                _logger.LogDebug("Run {RunId} status: {Status}", run.Value.Id, run.Value.Status);
            }
            while ((run.Value.Status == RunStatus.Queued || run.Value.Status == RunStatus.InProgress) && !cancellationToken.IsCancellationRequested);

            if (run.Value.Status != RunStatus.Completed)
            {
                _logger.LogError("Run {RunId} failed with status {Status}: {Error}", run.Value.Id, run.Value.Status, run.Value.LastError?.Message);
                throw new InvalidOperationException($"Run failed or was canceled: {run.Value.LastError?.Message}");
            }

            _logger.LogInformation("Run {RunId} completed successfully", run.Value.Id);

            // Get all messages and extract the assistant's response
            var messages = agentsClient.Messages.GetMessages(
                threadId,
                order: ListSortOrder.Descending, // Get newest first
                cancellationToken: cancellationToken);

            // Find the latest assistant message
            foreach (var threadMessage in messages)
            {
                // Check if this is an assistant message (not user message)
                if (threadMessage.Role != MessageRole.User)
                {
                    var responseText = new StringBuilder();
                    foreach (var contentItem in threadMessage.ContentItems)
                    {
                        if (contentItem is MessageTextContent textItem)
                        {
                            responseText.Append(textItem.Text);
                        }
                    }

                    if (responseText.Length > 0)
                    {
                        var finalResponse = responseText.ToString();
                        _logger.LogInformation("Azure AI Foundry agent {AgentId} generated response: {ResponseLength} characters",
                            _agentId, finalResponse.Length);

                        var result = new ChatMessageContent(AuthorRole.Assistant, finalResponse);
                        return [result];
                    }
                }
            }

            // If no assistant message found, return a default response
            _logger.LogWarning("No assistant response found from Azure AI Foundry agent {AgentId}", _agentId);
            var fallbackResponse = new ChatMessageContent(AuthorRole.Assistant, "I apologize, but I couldn't generate a response at this time.");
            return [fallbackResponse];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat message contents from Azure AI Foundry agent {AgentId}", _agentId);

            // Return a fallback response
            var fallbackResponse = new ChatMessageContent(AuthorRole.Assistant,
                "I'm sorry, I'm currently unable to process your request. Please try again later.");
            return [fallbackResponse];
        }
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_projectClient == null)
        {
            throw new InvalidOperationException("Service not initialized. Call InitializeAsync first.");
        }

        var userMessage = chatHistory.LastOrDefault(m => m.Role == AuthorRole.User)?.Content ?? string.Empty;

        // Get the full response first, then stream it
        var response = await GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
        var content = response.FirstOrDefault()?.Content ?? string.Empty;

        if (!string.IsNullOrEmpty(content))
        {
            // Simulate streaming by breaking response into chunks
            const int chunkSize = 10; // Number of characters per chunk
            for (int i = 0; i < content.Length; i += chunkSize)
            {
                var chunk = content.Substring(i, Math.Min(chunkSize, content.Length - i));
                yield return new StreamingChatMessageContent(AuthorRole.Assistant, chunk);

                // Small delay to simulate streaming
                await Task.Delay(50, cancellationToken);
            }
        }
        else
        {
            yield return new StreamingChatMessageContent(AuthorRole.Assistant,
                "I'm sorry, I'm currently unable to process your request. Please try again later.");
        }
    }
}