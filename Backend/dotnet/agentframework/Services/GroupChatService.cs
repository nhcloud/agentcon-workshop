using DotNetAgentFramework.Models;
using DotNetAgentFramework.Agents;
using Microsoft.Extensions.Options;
using DotNetAgentFramework.Configuration;

namespace DotNetAgentFramework.Services;

/// <summary>
/// Group chat service implementation using Microsoft Agent Framework
/// </summary>
public class GroupChatService : IGroupChatService
{
    private readonly IAgentService _agentService;
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<GroupChatService> _logger;
    private readonly AzureAIConfig _azureConfig;

    public GroupChatService(
        IAgentService agentService,
        ISessionManager sessionManager,
        ILogger<GroupChatService> logger,
        IOptions<AzureAIConfig> azureConfig)
    {
        _agentService = agentService;
        _sessionManager = sessionManager;
        _logger = logger;
        _azureConfig = azureConfig.Value;
    }

    /// <summary>
    /// Start a group chat with multiple agents using Microsoft Agent Framework
    /// </summary>
    public async Task<GroupChatResponse> StartGroupChatAsync(GroupChatRequest request)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        var cancellationToken = cts.Token;
        
        var sessionId = request.SessionId ?? await _sessionManager.CreateSessionAsync();
        var messages = new List<GroupChatMessage>();
        var terminatedAgents = new HashSet<string>();

        // Add the initial user message
        var userMessage = new GroupChatMessage
        {
            Content = request.Message,
            Agent = "user",
            Timestamp = DateTime.UtcNow,
            Turn = 0,
            MessageId = Guid.NewGuid().ToString()
        };
        messages.Add(userMessage);
        await _sessionManager.AddMessageToSessionAsync(sessionId, userMessage);

        var currentTurn = 1;
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting Agent Framework group chat with {AgentCount} agents, max turns: {MaxTurns}", 
                request.Agents?.Count ?? 0, request.MaxTurns);

            // Process agents in sequence for each turn
            for (int turn = 1; turn <= request.MaxTurns; turn++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var agentName in request.Agents ?? new List<string>())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Skip agents that have terminated
                    if (terminatedAgents.Contains(agentName))
                    {
                        _logger.LogInformation("Agent {AgentName} has terminated, skipping", agentName);
                        continue;
                    }

                    var agentStartTime = DateTime.UtcNow;
                    var agent = await _agentService.GetAgentAsync(agentName);
                    if (agent == null)
                    {
                        _logger.LogWarning("Agent {AgentName} not found, skipping", agentName);
                        continue;
                    }

                    // Build context for the agent
                    var agentContext = BuildAgentContext(messages, agentName, request.Message);

                    try
                    {
                        // Get agent response with timeout
                        using var agentCts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, agentCts.Token);

                        var response = await agent.RespondAsync(request.Message, agentContext);

                        // Check if agent terminated
                        bool isTerminated = IsAgentTerminated(response);
                        if (isTerminated)
                        {
                            terminatedAgents.Add(agentName);
                            _logger.LogInformation("Agent {AgentName} terminated from conversation", agentName);
                        }

                        var agentMessage = new GroupChatMessage
                        {
                            Content = response,
                            Agent = agentName,
                            Timestamp = DateTime.UtcNow,
                            Turn = currentTurn,
                            MessageId = Guid.NewGuid().ToString(),
                            IsTerminated = isTerminated,
                            AgentType = "Agent Framework"
                        };

                        messages.Add(agentMessage);
                        await _sessionManager.AddMessageToSessionAsync(sessionId, agentMessage);

                        var processingTime = (DateTime.UtcNow - agentStartTime).TotalSeconds;
                        _logger.LogInformation("Agent {AgentName} responded in turn {Turn} ({ProcessingTime:F1}s), terminated: {IsTerminated}",
                            agentName, currentTurn, processingTime, isTerminated);
                        currentTurn++;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Agent {AgentName} timed out, marking as terminated", agentName);
                        terminatedAgents.Add(agentName);

                        var timeoutMessage = new GroupChatMessage
                        {
                            Content = "TERMINATED - Agent response timed out",
                            Agent = agentName,
                            Timestamp = DateTime.UtcNow,
                            Turn = currentTurn,
                            MessageId = Guid.NewGuid().ToString(),
                            IsTerminated = true,
                            AgentType = "Agent Framework"
                        };
                        messages.Add(timeoutMessage);
                        currentTurn++;
                    }
                }

                // Check if all agents have terminated
                if (terminatedAgents.Count == request.Agents?.Count)
                {
                    _logger.LogInformation("All agents have terminated, ending group chat");
                    break;
                }
            }

            // Generate summary
            string? summary = null;
            try
            {
                using var summaryCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, summaryCts.Token);
                summary = await SummarizeConversationAsync(messages, combinedCts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Conversation summary timed out, skipping");
                summary = "Summary generation timed out - conversation completed successfully.";
            }

            var totalProcessingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            return new GroupChatResponse
            {
                Messages = messages,
                SessionId = sessionId,
                TotalTurns = currentTurn - 1,
                Summary = summary,
                GroupChatType = "Agent Framework Group Chat",
                AgentCount = request.Agents?.Count ?? 0,
                TerminatedAgents = terminatedAgents.ToList(),
                TotalProcessingTimeMs = totalProcessingTime
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Group chat cancelled due to timeout for session {SessionId}", sessionId);
            throw new TimeoutException("Group chat operation timed out. Please try with fewer agents or reduced max_turns.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during group chat for session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Summarize a conversation between multiple agents
    /// </summary>
    public async Task<string> SummarizeConversationAsync(List<GroupChatMessage> messages)
    {
        return await SummarizeConversationAsync(messages, CancellationToken.None);
    }

    /// <summary>
    /// Summarize a conversation with cancellation token support
    /// </summary>
    public async Task<string> SummarizeConversationAsync(List<GroupChatMessage> messages, CancellationToken cancellationToken = default)
    {
        try
        {
            if (messages.Count <= 1)
            {
                return "No meaningful conversation to summarize.";
            }

            var agentMessages = messages.Where(m => m.Agent != "user").ToList();
            if (!agentMessages.Any())
            {
                return "No agent responses to summarize.";
            }

            var userMessage = messages.FirstOrDefault(m => m.Agent == "user")?.Content ?? "No user message";
            
            // Create a comprehensive summary
            var agentNames = agentMessages.Select(m => m.Agent).Distinct().ToList();
            var totalMessages = agentMessages.Count;
            
            // Build a structured summary
            var summary = $"**Agent Framework Group Chat Summary**\n\n";
            summary += $"**Original Question**: {userMessage}\n\n";
            summary += $"**Participants**: {string.Join(", ", agentNames)} ({agentNames.Count} agents)\n";
            summary += $"**Total Responses**: {totalMessages}\n\n";
            
            summary += "**Key Contributions**:\n";
            foreach (var agentName in agentNames)
            {
                var agentMessagesForAgent = agentMessages.Where(m => m.Agent == agentName).ToList();
                var lastMessage = agentMessagesForAgent.LastOrDefault()?.Content;
                if (!string.IsNullOrEmpty(lastMessage))
                {
                    var preview = lastMessage.Length > 100 ? lastMessage.Substring(0, 100) + "..." : lastMessage;
                    summary += $"• **{agentName}**: {preview}\n";
                }
            }

            summary += $"\n**Conversation completed using Microsoft Agent Framework with {totalMessages} total responses from {agentNames.Count} agents.**";

            return summary;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Summary generation was cancelled");
            return "Summary generation was cancelled due to timeout.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating conversation summary");
            return "Error generating summary - please review the conversation manually.";
        }
    }

    /// <summary>
    /// Check if an agent response indicates termination
    /// </summary>
    private static bool IsAgentTerminated(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return false;

        return response.Trim().StartsWith("TERMINATED", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Build context for an agent based on the conversation history
    /// </summary>
    private string BuildAgentContext(List<GroupChatMessage> currentMessages, string agentName, string userMessage)
    {
        var context = $"?? **User's Original Question**: {userMessage}\n\n";
        
        if (currentMessages.Count > 1)
        {
            var previousResponses = currentMessages
                .Where(m => m.Agent != "user" && m.Agent != agentName)
                .OrderBy(m => m.Turn)
                .Select(m => $"**{m.Agent}** (Turn {m.Turn}): {m.Content}")
                .ToList();

            if (previousResponses.Any())
            {
                context += $"?? **Other Agent Responses**:\n{string.Join("\n\n", previousResponses)}\n\n";
                context += $"?? **Your Role as {agentName}**:\n";
                context += "- Provide your unique perspective and expertise\n";
                context += "- Build upon or complement what others have said\n";
                context += "- Avoid repeating points already covered\n";
                context += "- Add new insights from your specialization\n";
                context += "- Be comprehensive yet concise\n\n";
                context += "Focus on what you can uniquely contribute to answer the user's question.";
            }
        }
        else
        {
            context += $"?? You are the first agent to respond. Provide a comprehensive answer from your {agentName} perspective.";
        }

        return context;
    }
}