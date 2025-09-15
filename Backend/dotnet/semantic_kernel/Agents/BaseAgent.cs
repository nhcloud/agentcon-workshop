using DotNetSemanticKernel.Models;
using Microsoft.SemanticKernel;

namespace DotNetSemanticKernel.Agents;

public interface IAgent
{
    string Name { get; }
    string Description { get; }
    string Instructions { get; }
    Task<string> RespondAsync(string message, string? context = null);
    Task<ChatResponse> ChatAsync(ChatRequest request);
}

public abstract class BaseAgent : IAgent
{
    protected readonly Kernel _kernel;
    protected readonly ILogger _logger;

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Instructions { get; }

    protected BaseAgent(Kernel kernel, ILogger logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public virtual async Task<string> RespondAsync(string message, string? context = null)
    {
        try
        {
            var systemPrompt = Instructions;
            if (!string.IsNullOrEmpty(context))
            {
                systemPrompt += $"\n\nContext: {context}";
            }

            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage(message);

            var chatCompletion = _kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentsAsync(chatHistory);

            return result.LastOrDefault()?.Content ?? "I apologize, but I couldn't generate a response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {AgentName} responding to message", Name);
            return $"Error: {ex.Message}";
        }
    }

    public virtual async Task<ChatResponse> ChatAsync(ChatRequest request)
    {
        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
        var content = await RespondAsync(request.Message);

        return new ChatResponse
        {
            Content = content,
            Agent = Name,
            SessionId = sessionId,
            Usage = new UsageInfo
            {
                PromptTokens = EstimateTokens(request.Message),
                CompletionTokens = EstimateTokens(content),
                TotalTokens = EstimateTokens(request.Message) + EstimateTokens(content)
            }
        };
    }

    protected virtual int EstimateTokens(string text)
    {
        // Simple token estimation (roughly 4 characters per token)
        return text.Length / 4;
    }
}