using DotNetSemanticKernel.Models;
using DotNetSemanticKernel.Agents;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace DotNetSemanticKernel.Services;

public interface IGroupChatService
{
    Task<GroupChatResponse> StartGroupChatAsync(GroupChatRequest request);
    Task<GroupChatResponse> StartSemanticKernelGroupChatAsync(GroupChatRequest request);
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

    /// <summary>
    /// Standard group chat implementation
    /// </summary>
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

        var currentTurn = 1;

        try
        {
            // Process each agent in sequence for each turn
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

                // Check if we should continue
                if (messages.Count >= request.Agents.Count * request.MaxTurns + 1)
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

    /// <summary>
    /// Advanced group chat using Semantic Kernel's AgentGroupChat
    /// This matches the Python Semantic Kernel implementation patterns
    /// </summary>
    public async Task<GroupChatResponse> StartSemanticKernelGroupChatAsync(GroupChatRequest request)
    {
        var sessionId = request.SessionId ?? await _sessionManager.CreateSessionAsync();
        var messages = new List<GroupChatMessage>();

        try
        {
            // Create agents for the group chat
            var chatAgents = new List<ChatCompletionAgent>();
            var agentMap = new Dictionary<string, string>();

            foreach (var agentName in request.Agents)
            {
                var agent = await _agentService.GetAgentAsync(agentName);
                if (agent is BaseAgent baseAgent && baseAgent._chatAgent != null)
                {
                    chatAgents.Add(baseAgent._chatAgent);
                    agentMap[baseAgent._chatAgent.Id] = agentName;
                }
                else
                {
                    _logger.LogWarning("Agent {AgentName} not compatible with SK GroupChat", agentName);
                }
            }

            if (!chatAgents.Any())
            {
                throw new InvalidOperationException("No compatible agents found for Semantic Kernel group chat");
            }

            // For now, fall back to standard group chat since AgentGroupChat is experimental
            // In future SK versions, this would use:
            // var groupChat = new AgentGroupChat(chatAgents.ToArray())
            _logger.LogInformation("AgentGroupChat is experimental, using enhanced standard group chat");
            
            return await StartEnhancedGroupChatAsync(request, chatAgents, agentMap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SK group chat for session {SessionId}", sessionId);
            
            // Fallback to standard group chat
            _logger.LogInformation("Falling back to standard group chat implementation");
            return await StartGroupChatAsync(request);
        }
    }

    /// <summary>
    /// Enhanced group chat that simulates AgentGroupChat behavior
    /// </summary>
    private async Task<GroupChatResponse> StartEnhancedGroupChatAsync(
        GroupChatRequest request, 
        List<ChatCompletionAgent> chatAgents, 
        Dictionary<string, string> agentMap)
    {
        var sessionId = request.SessionId ?? await _sessionManager.CreateSessionAsync();
        var messages = new List<GroupChatMessage>();
        
        // Add initial user message
        var userMessage = new GroupChatMessage
        {
            Content = request.Message,
            Agent = "user",
            Timestamp = DateTime.UtcNow,
            Turn = 0
        };
        messages.Add(userMessage);

        // Create enhanced context for group interaction
        var conversationContext = $"You are participating in a group discussion with {chatAgents.Count} other AI agents. ";
        conversationContext += $"Each agent brings unique expertise. Coordinate with others and build upon their insights. ";
        conversationContext += $"Original question: {request.Message}";

        // Simulate group chat with enhanced coordination
        var currentTurn = 1;
        for (int turn = 1; turn <= request.MaxTurns; turn++)
        {
            foreach (var chatAgent in chatAgents)
            {
                if (agentMap.TryGetValue(chatAgent.Id, out var agentName))
                {
                    var agent = await _agentService.GetAgentAsync(agentName);
                    if (agent == null) continue;

                    // Build context with previous agent responses
                    var agentContext = BuildAdvancedAgentContext(messages, agentName, request.Message, conversationContext);
                    
                    var response = await agent.RespondAsync(request.Message, agentContext);
                    
                    var agentMessage = new GroupChatMessage
                    {
                        Content = response,
                        Agent = agentName,
                        Timestamp = DateTime.UtcNow,
                        Turn = currentTurn,
                        AgentType = "Enhanced",
                        MessageId = Guid.NewGuid().ToString()
                    };

                    messages.Add(agentMessage);
                    await _sessionManager.AddMessageToSessionAsync(sessionId, agentMessage);
                    
                    _logger.LogInformation("Enhanced GroupChat: Agent {AgentName} responded in turn {Turn}", agentName, currentTurn);
                    currentTurn++;
                }
            }
        }

        // Generate summary
        var summary = await SummarizeConversationAsync(messages);

        return new GroupChatResponse
        {
            Messages = messages,
            SessionId = sessionId,
            TotalTurns = currentTurn - 1,
            Summary = summary,
            GroupChatType = "Enhanced Semantic Kernel GroupChat Simulation",
            AgentCount = chatAgents.Count
        };
    }

    private string BuildAdvancedAgentContext(List<GroupChatMessage> currentMessages, string agentName, string userMessage, string conversationContext)
    {
        var context = $"{conversationContext}\n\n";
        context += $"?? **User's Question**: {userMessage}\n\n";
        
        if (currentMessages.Count > 1)
        {
            var previousResponses = currentMessages
                .Where(m => m.Agent != "user" && m.Agent != agentName)
                .OrderBy(m => m.Turn)
                .Select(m => $"**{m.Agent}** (Turn {m.Turn}): {m.Content}")
                .ToList();

            if (previousResponses.Any())
            {
                context += $"??? **Previous Agent Contributions**:\n{string.Join("\n\n", previousResponses)}\n\n";
                context += $"?? **Your Role as {agentName}**:\n";
                context += "- Acknowledge and reference relevant points from other agents\n";
                context += "- Provide your unique perspective and expertise\n";
                context += "- Build upon the conversation constructively\n";
                context += "- Avoid unnecessary repetition\n";
                context += "- Contribute new insights from your specialization\n\n";
                context += "Work collaboratively to provide a comprehensive response to the user.";
            }
        }
        else
        {
            context += $"?? You are the first agent to respond in this group discussion. Set a strong foundation for other agents to build upon.";
        }

        return context;
    }

    public async Task<string> SummarizeConversationAsync(List<GroupChatMessage> messages)
    {
        try
        {
            if (messages.Count <= 1)
            {
                return "No meaningful conversation to summarize.";
            }

            var conversationText = string.Join("\n", 
                messages.Where(m => m.Agent != "user")
                       .Select(m => $"{m.Agent}: {m.Content}"));
            
            var userMessage = messages.FirstOrDefault(m => m.Agent == "user")?.Content ?? "No user message";

            var systemPrompt = @"You are an expert conversation summarizer specializing in multi-agent discussions. 

Analyze the following conversation between AI agents responding to a user's question and provide a comprehensive summary that includes:

?? **Main Topic**: What was the user asking about?
?? **Key Insights**: The most important points raised by each agent
?? **Unique Perspectives**: How each agent approached the question differently  
?? **Consensus & Disagreements**: Areas where agents aligned or differed
?? **Actionable Takeaways**: Practical next steps or recommendations
? **Overall Quality**: How well the agents addressed the user's needs

Format your summary clearly with the above sections. Be concise but thorough.";

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage($@"User's Question: {userMessage}

Agent Responses:
{conversationText}

Please provide a comprehensive summary following the requested format.");

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentsAsync(chatHistory);

            return result.LastOrDefault()?.Content ?? "Unable to generate summary.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating conversation summary");
            return "Error generating summary - please review the conversation manually.";
        }
    }

    private string BuildAgentContext(List<GroupChatMessage> currentMessages, string agentName, string userMessage)
    {
        var context = $"?? **User's Original Question**: {userMessage}\n\n";
        
        if (currentMessages.Count > 1)
        {
            var previousResponses = currentMessages
                .Where(m => m.Agent != "user" && m.Agent != agentName)
                .Select(m => $"**{m.Agent}**: {m.Content}")
                .ToList();

            if (previousResponses.Any())
            {
                context += $"??? **Other Agent Responses**:\n{string.Join("\n\n", previousResponses)}\n\n";
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