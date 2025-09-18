using DotNetSemanticKernel.Models;
using DotNetSemanticKernel.Agents;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
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
            Turn = 0,
            MessageId = Guid.NewGuid().ToString()
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
                        Turn = currentTurn,
                        MessageId = Guid.NewGuid().ToString()
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
                Summary = summary,
                GroupChatType = "Standard Group Chat",
                AgentCount = request.Agents.Count
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
    /// This uses the native SK AgentGroupChat functionality
    /// </summary>
    public async Task<GroupChatResponse> StartSemanticKernelGroupChatAsync(GroupChatRequest request)
    {
        var sessionId = request.SessionId ?? await _sessionManager.CreateSessionAsync();
        var messages = new List<GroupChatMessage>();

        try
        {
            // Add initial user message
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
                    _logger.LogInformation("Added agent {AgentName} to Semantic Kernel GroupChat", agentName);
                }
                else
                {
                    _logger.LogWarning("Agent {AgentName} not compatible with SK GroupChat, creating enhanced version", agentName);
                    
                    // Create a compatible ChatCompletionAgent for non-BaseAgent types
                    var compatibleAgent = new ChatCompletionAgent()
                    {
                        Name = agentName,
                        Instructions = agent?.Instructions ?? $"You are {agentName}, providing specialized assistance.",
                        Kernel = _kernel,
                        Arguments = new KernelArguments()
                    };
                    chatAgents.Add(compatibleAgent);
                    agentMap[compatibleAgent.Id] = agentName;
                }
            }

            if (!chatAgents.Any())
            {
                throw new InvalidOperationException("No compatible agents found for Semantic Kernel group chat");
            }

            // Create AgentGroupChat with smart termination strategy
#pragma warning disable SKEXP0110 // Suppress experimental API warning
            var groupChat = new AgentGroupChat(chatAgents.ToArray())
            {
                ExecutionSettings = new()
                {
                    TerminationStrategy = new SmartTerminationStrategy(request.MaxTurns, chatAgents.Count, _kernel, _logger)
                }
            };
#pragma warning restore SKEXP0110

            _logger.LogInformation("Created Semantic Kernel AgentGroupChat with {AgentCount} agents and smart termination", chatAgents.Count);

            // Add user message to the group chat
            groupChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, request.Message));

            var currentTurn = 1;
            var iterationCount = 0;
            var maxIterations = request.MaxTurns * chatAgents.Count;

            // Process the group chat
            await foreach (var chatMessage in groupChat.InvokeAsync())
            {
                iterationCount++;
                
                // Map the agent ID back to our agent name
                var agentId = chatMessage.AuthorName ?? "unknown";
                var agentName = agentMap.ContainsKey(agentId) ? agentMap[agentId] : agentId;

                var agentMessage = new GroupChatMessage
                {
                    Content = chatMessage.Content ?? "",
                    Agent = agentName,
                    Timestamp = DateTime.UtcNow,
                    Turn = currentTurn,
                    AgentType = "Semantic Kernel AgentGroupChat",
                    MessageId = Guid.NewGuid().ToString()
                };

                messages.Add(agentMessage);
                await _sessionManager.AddMessageToSessionAsync(sessionId, agentMessage);
                
                _logger.LogInformation("SK GroupChat: Agent {AgentName} responded in iteration {Iteration}", agentName, iterationCount);
                currentTurn++;

                // Check termination conditions
                if (iterationCount >= maxIterations)
                {
                    _logger.LogInformation("SK GroupChat terminated after {Iterations} iterations (max reached)", iterationCount);
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
                Summary = summary,
                GroupChatType = "Semantic Kernel AgentGroupChat",
                AgentCount = chatAgents.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SK group chat for session {SessionId}", sessionId);
            
            // Fallback to enhanced group chat
            _logger.LogInformation("Falling back to enhanced group chat implementation");
            return await StartEnhancedGroupChatAsync(request);
        }
    }

    /// <summary>
    /// Enhanced group chat that simulates AgentGroupChat behavior when SK GroupChat fails
    /// </summary>
    private async Task<GroupChatResponse> StartEnhancedGroupChatAsync(GroupChatRequest request)
    {
        var sessionId = request.SessionId ?? await _sessionManager.CreateSessionAsync();
        var messages = new List<GroupChatMessage>();
        
        // Add initial user message
        var userMessage = new GroupChatMessage
        {
            Content = request.Message,
            Agent = "user",
            Timestamp = DateTime.UtcNow,
            Turn = 0,
            MessageId = Guid.NewGuid().ToString()
        };
        messages.Add(userMessage);

        // Create enhanced context for group interaction
        var conversationContext = $"You are participating in a group discussion with {request.Agents.Count} other AI agents. ";
        conversationContext += $"Each agent brings unique expertise. Coordinate with others and build upon their insights. ";
        conversationContext += $"Original question: {request.Message}";

        // Simulate group chat with enhanced coordination
        var currentTurn = 1;
        for (int turn = 1; turn <= request.MaxTurns; turn++)
        {
            foreach (var agentName in request.Agents)
            {
                var agent = await _agentService.GetAgentAsync(agentName);
                if (agent == null)
                {
                    _logger.LogWarning("Agent {AgentName} not found in enhanced group chat", agentName);
                    continue;
                }

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

        // Generate summary
        var summary = await SummarizeConversationAsync(messages);

        return new GroupChatResponse
        {
            Messages = messages,
            SessionId = sessionId,
            TotalTurns = currentTurn - 1,
            Summary = summary,
            GroupChatType = "Enhanced Group Chat (SK Fallback)",
            AgentCount = request.Agents.Count
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
?? **Overall Quality**: How well the agents addressed the user's needs

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

/// <summary>
/// Smart termination strategy that considers conversation quality and convergence
/// </summary>
#pragma warning disable SKEXP0110 // Suppress experimental API warning
public class SmartTerminationStrategy : TerminationStrategy
{
    private readonly int _maxTurns;
    private readonly int _agentCount;
    private readonly Kernel _kernel;
    private readonly int _minResponses;
    private readonly ILogger? _logger;

    public SmartTerminationStrategy(int maxTurns, int agentCount, Kernel kernel, ILogger? logger = null)
    {
        _maxTurns = maxTurns;
        _agentCount = agentCount;
        _kernel = kernel;
        _minResponses = Math.Max(1, agentCount); // Ensure at least one response per agent
        _logger = logger;
    }

    protected override async Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken = default)
    {
        // Count agent messages (exclude user messages)
        var agentMessages = history.Where(m => m.AuthorName != "user" && !string.IsNullOrEmpty(m.AuthorName)).ToList();
        var messageCount = agentMessages.Count;

        _logger?.LogDebug("Smart termination check: {MessageCount} messages, min: {MinResponses}, max: {MaxIterations}", 
            messageCount, _minResponses, _maxTurns * _agentCount);

        // Never terminate before minimum responses
        if (messageCount < _minResponses)
        {
            _logger?.LogDebug("Continuing: Not enough responses yet ({MessageCount} < {MinResponses})", messageCount, _minResponses);
            return false;
        }

        // Hard limit: terminate if max iterations reached
        var maxIterations = _maxTurns * _agentCount;
        if (messageCount >= maxIterations)
        {
            _logger?.LogInformation("Terminating: Max iterations reached ({MessageCount} >= {MaxIterations})", messageCount, maxIterations);
            return true;
        }

        // Early termination if we have at least one round and conversation seems complete
        if (messageCount >= _agentCount)
        {
            try
            {
                // Analyze conversation quality and convergence
                var shouldTerminate = await AnalyzeConversationConvergence(agentMessages, cancellationToken);
                _logger?.LogInformation("Smart termination analysis result: {ShouldTerminate} after {MessageCount} messages", shouldTerminate, messageCount);
                return shouldTerminate;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Smart termination analysis failed, continuing conversation");
                // If analysis fails, continue with default behavior
                return false;
            }
        }

        return false;
    }

    private async Task<bool> AnalyzeConversationConvergence(IEnumerable<ChatMessageContent> agentMessages, CancellationToken cancellationToken)
    {
        try
        {
            var messages = agentMessages.ToList();
            if (messages.Count < 2) return false;

            // Get recent messages for analysis
            var recentMessages = messages.TakeLast(Math.Min(6, messages.Count)).ToList();
            var conversationText = string.Join("\n", recentMessages.Select(m => $"{m.AuthorName}: {m.Content}"));

            var systemPrompt = @"You are an expert conversation analyzer. Analyze this AI agent conversation and determine if it should continue or terminate.

Consider these factors:
1. Are agents providing NEW valuable information?
2. Are agents repeating similar points?
3. Has the original question been adequately addressed?
4. Are agents adding unique perspectives or just rephrasing?
5. Is the conversation converging toward a solution?

Respond with ONLY 'CONTINUE' or 'TERMINATE' followed by a brief reason.

Examples:
- 'TERMINATE - Agents are repeating similar information without adding value'
- 'CONTINUE - Each agent is providing unique valuable insights'
- 'TERMINATE - Question has been comprehensively answered'
- 'CONTINUE - Conversation is building toward a more complete solution'\";

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage($"Recent conversation:\n{conversationText}");

            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletion.GetChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken);
            
            var analysis = result.LastOrDefault()?.Content?.Trim() ?? "";
            
            // Parse the response
            var shouldTerminate = analysis.StartsWith("TERMINATE", StringComparison.OrdinalIgnoreCase);
            
            _logger?.LogDebug("Conversation analysis: {Analysis}", analysis);
            
            return shouldTerminate;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to analyze conversation convergence");
            // If analysis fails, default to continuing (conservative approach)
            return false;
        }
    }
}
#pragma warning restore SKEXP0110

/// <summary>
/// Simple count-based termination strategy for AgentGroupChat (fallback)
/// </summary>
#pragma warning disable SKEXP0110 // Suppress experimental API warning
public class CountBasedTerminationStrategy : TerminationStrategy
{
    private readonly int _maxIterations;

    public CountBasedTerminationStrategy(int maxIterations)
    {
        _maxIterations = maxIterations;
    }

    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken = default)
    {
        // Count agent messages (exclude user messages)
        var agentMessages = history.Count(m => m.AuthorName != "user" && !string.IsNullOrEmpty(m.AuthorName));
        return Task.FromResult(agentMessages >= _maxIterations);
    }
}
#pragma warning restore SKEXP0110