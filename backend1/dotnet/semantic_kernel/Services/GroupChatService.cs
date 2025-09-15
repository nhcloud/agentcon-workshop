using DotNetSemanticKernel.Models;
using DotNetSemanticKernel.Agents;
using Microsoft.SemanticKernel;

namespace DotNetSemanticKernel.Services;

public interface IGroupChatService
{
    Task<GroupChatResponse> StartGroupChatAsync(GroupChatRequest request);
    Task<string> SummarizeConversationAsync(List<GroupChatMessage> messages);
}

public class GroupChatService : IGroupChatService
{
    private readonly IAgentService _agentService;
    private readonly ISessionManager _sessionManager;
    private readonly Kernel _kernel;
    private readonly ILogger<GroupChatService> _logger;

    public GroupChatService(
        IAgentService agentService, 
        ISessionManager sessionManager, 
        Kernel kernel,
        ILogger<GroupChatService> logger)
    {
        _agentService = agentService;
        _sessionManager = sessionManager;
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<GroupChatResponse> StartGroupChatAsync(GroupChatRequest request)
    {
        var sessionId = request.SessionId ?? await _sessionManager.CreateSessionAsync();
        var messages = new List<GroupChatMessage>();
        
        // Add the initial user message
        var userMessage = new GroupChatMessage
        {
            Content = request.Message,
            Agent = "user",
            Timestamp = DateTime.UtcNow,
            Turn = 0
        };
        messages.Add(userMessage);
        await _sessionManager.AddMessageToSessionAsync(sessionId, userMessage);

        var context = string.Empty;
        var currentTurn = 1;

        try
        {
            // Get session history for context
            var history = await _sessionManager.GetSessionHistoryAsync(sessionId);
            if (history.Count > 1)
            {
                context = BuildContextFromHistory(history.TakeLast(10).ToList());
            }

            // Process each agent in sequence
            for (int turn = 1; turn <= request.MaxTurns; turn++)
            {
                foreach (var agentName in request.Agents)
                {
                    var agent = await _agentService.GetAgentAsync(agentName);
                    if (agent == null)
                    {
                        _logger.LogWarning("Agent {AgentName} not found, skipping", agentName);
                        continue;
                    }

                    // Prepare context for the agent
                    var agentContext = BuildAgentContext(messages, agentName, request.Message);
                    
                    // Get agent response
                    var response = await agent.RespondAsync(request.Message, agentContext);
                    
                    var agentMessage = new GroupChatMessage
                    {
                        Content = response,
                        Agent = agentName,
                        Timestamp = DateTime.UtcNow,
                        Turn = currentTurn
                    };

                    messages.Add(agentMessage);
                    await _sessionManager.AddMessageToSessionAsync(sessionId, agentMessage);
                    
                    _logger.LogInformation("Agent {AgentName} responded in turn {Turn}", agentName, currentTurn);
                    currentTurn++;
                }

                // Check if we should continue (simple logic - can be enhanced)
                if (turn >= request.MaxTurns || messages.Count >= request.Agents.Count * request.MaxTurns + 1)
                {
                    break;
                }
            }

            // Generate summary
            var summary = await SummarizeConversationAsync(messages);

            return new GroupChatResponse
            {
                Messages = messages,
                SessionId = sessionId,
                TotalTurns = currentTurn - 1,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during group chat for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<string> SummarizeConversationAsync(List<GroupChatMessage> messages)
    {
        try
        {
            if (messages.Count <= 1)
            {
                return "No conversation to summarize.";
            }

            var conversationText = string.Join("\n", messages.Select(m => $"{m.Agent}: {m.Content}"));
            
            var systemPrompt = @"You are a conversation summarizer. Please provide a concise summary of the following conversation between multiple agents. 
Focus on:
- Key topics discussed
- Main insights or conclusions
- Different perspectives from each agent
- Action items or decisions made

Keep the summary brief but informative.";

            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage($"Please summarize this conversation:\n\n{conversationText}");

            var chatCompletion = _kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentsAsync(chatHistory);

            return result.LastOrDefault()?.Content ?? "Unable to generate summary.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating conversation summary");
            return "Error generating summary.";
        }
    }

    private string BuildContextFromHistory(List<GroupChatMessage> history)
    {
        if (!history.Any()) return string.Empty;

        var contextMessages = history.TakeLast(5).Select(m => $"{m.Agent}: {m.Content}").ToList();
        return $"Recent conversation context:\n{string.Join("\n", contextMessages)}";
    }

    private string BuildAgentContext(List<GroupChatMessage> currentMessages, string agentName, string userMessage)
    {
        var context = $"User's original question: {userMessage}\n\n";
        
        if (currentMessages.Count > 1)
        {
            var previousResponses = currentMessages
                .Where(m => m.Agent != "user" && m.Agent != agentName)
                .Select(m => $"{m.Agent}: {m.Content}")
                .ToList();

            if (previousResponses.Any())
            {
                context += $"Other agents have already responded:\n{string.Join("\n", previousResponses)}\n\n";
                context += $"As the {agentName} agent, provide your unique perspective and insights. ";
                context += "Build upon or complement what others have said, but don't repeat their points.";
            }
        }

        return context;
    }
}