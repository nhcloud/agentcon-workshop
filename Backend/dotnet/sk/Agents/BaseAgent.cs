using DotNetSemanticKernel.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DotNetSemanticKernel.Agents;

public interface IAgent
{
    string Name { get; }
    string Description { get; }
    string Instructions { get; }
    Task<string> RespondAsync(string message, string? context = null);
    Task<ChatResponse> ChatAsync(ChatRequest request);
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
        if (_chatAgent == null)
        {
            await InitializeAsync();
        }

        try
        {
            // Create chat history similar to Python implementation
            var chatHistory = new ChatHistory();
            
            // Add system message with instructions
            var systemMessage = Instructions;
            if (!string.IsNullOrEmpty(context))
            {
                systemMessage += $"\n\nAdditional Context: {context}";
            }
            chatHistory.AddSystemMessage(systemMessage);
            
            // Add user message
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
        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;
        
        var content = await RespondAsync(request.Message, request.Context);
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
/// Azure AI Foundry Agent that connects to pre-existing agents in Azure AI Foundry
/// This matches the Python implementation structure
/// </summary>
public class AzureAIFoundryAgent : BaseAgent
{
    private readonly string _agentId;
    private readonly string _projectEndpoint;

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
        ILogger<AzureAIFoundryAgent> logger) 
        : base(kernel, logger)
    {
        Name = name;
        _agentId = agentId;
        _projectEndpoint = projectEndpoint;
        Description = description;
        Instructions = instructions;
    }

    public override async Task InitializeAsync()
    {
        try
        {
            // For Azure AI Foundry agents, we create a specialized ChatCompletionAgent
            // that can connect to the pre-existing agent in Azure AI Foundry
            _chatAgent = new ChatCompletionAgent()
            {
                Name = Name,
                Instructions = $"{Instructions}\n\nAzure AI Foundry Agent ID: {_agentId}\nProject: {_projectEndpoint}",
                Kernel = _kernel,
                Arguments = new KernelArguments()
            };
            
            _logger.LogInformation("Initialized Azure AI Foundry agent {AgentName} with ID {AgentId}", Name, _agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure AI Foundry agent {AgentName}", Name);
            throw;
        }
    }

    public override async Task<string> RespondAsync(string message, string? context = null)
    {
        if (_chatAgent == null)
        {
            await InitializeAsync();
        }

        try
        {
            // Enhanced context for Azure AI Foundry agents
            var enhancedContext = $"Azure AI Foundry Agent: {Name} (ID: {_agentId})";
            if (!string.IsNullOrEmpty(context))
            {
                enhancedContext += $"\nContext: {context}";
            }

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage($"{Instructions}\n\n{enhancedContext}");
            chatHistory.AddUserMessage(message);

            var responses = new List<ChatMessageContent>();
            await foreach (var content in _chatAgent!.InvokeAsync(chatHistory))
            {
                responses.Add(content);
            }

            var lastResponse = responses.LastOrDefault();
            return lastResponse?.Content ?? "I apologize, but I couldn't generate a response from the Azure AI Foundry agent.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Azure AI Foundry agent {AgentName} responding to message", Name);
            return $"Azure AI Foundry agent error: {ex.Message}";
        }
    }

    public override async Task<ChatResponse> ChatAsync(ChatRequest request)
    {
        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;
        
        var content = await RespondAsync(request.Message, request.Context);
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