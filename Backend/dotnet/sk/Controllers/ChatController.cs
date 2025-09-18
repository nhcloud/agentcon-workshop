using DotNetSemanticKernel.Models;
using DotNetSemanticKernel.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetSemanticKernel.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ISessionManager _sessionManager;
    private readonly IGroupChatService _groupChatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IAgentService agentService, 
        ISessionManager sessionManager,
        IGroupChatService groupChatService,
        ILogger<ChatController> logger)
    {
        _agentService = agentService;
        _sessionManager = sessionManager;
        _groupChatService = groupChatService;
        _logger = logger;
    }

    /// <summary>
    /// Process a chat message - handles both single and multiple agents
    /// Frontend payload: { message, session_id?, agents? }
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<object>> Chat([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { detail = "Message is required" });
            }

            // Generate session ID if not provided (matching frontend expectation)
            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();

            // Retrieve conversation history for the session
            List<GroupChatMessage> conversationHistory = new();
            if (!string.IsNullOrEmpty(request.SessionId))
            {
                try
                {
                    conversationHistory = await _sessionManager.GetSessionHistoryAsync(request.SessionId);
                    _logger.LogInformation("Retrieved {MessageCount} messages from session {SessionId}", conversationHistory.Count, request.SessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve session history for {SessionId}, starting fresh", request.SessionId);
                    conversationHistory = new List<GroupChatMessage>();
                }
            }

            // Check if multiple agents were specified (frontend sends agents array)
            if (request.Agents != null && request.Agents.Count > 1)
            {
                // Route to group chat for multiple agents
                var groupRequest = new GroupChatRequest
                {
                    Message = request.Message,
                    Agents = request.Agents,
                    SessionId = sessionId,
                    MaxTurns = 1,
                    UseSemanticKernelGroupChat = false
                };

                var groupResponse = await _groupChatService.StartGroupChatAsync(groupRequest);
                var responseMessages = groupResponse.Messages?.Where(m => m.Agent != "user").ToList() ?? new List<GroupChatMessage>();
                var lastMessage = responseMessages.LastOrDefault();

                // Return frontend-compatible group chat response
                return Ok(new
                {
                    content = lastMessage?.Content ?? "No response generated",
                    agent = lastMessage?.Agent ?? "system",
                    session_id = sessionId,
                    timestamp = DateTime.UtcNow.ToString("O"),
                    metadata = new { 
                        total_agents = request.Agents.Count,
                        group_chat = true,
                        all_responses = responseMessages.Select(m => new { agent = m.Agent, content = m.Content }).ToList(),
                        conversation_length = conversationHistory.Count
                    }
                });
            }

            // Single agent handling with conversation history
            var agentName = request.Agents?.FirstOrDefault() ?? request.Agent ?? "generic";
            
            _logger.LogInformation("Chat request for agent {AgentName} with {HistoryCount} previous messages: {Message}", 
                agentName, conversationHistory.Count, request.Message);

            // Filter conversation history to only include relevant messages for this agent
            var relevantHistory = conversationHistory
                .Where(m => m.Agent == "user" || m.Agent == agentName)
                .ToList();

            var response = await _agentService.ChatWithAgentAsync(agentName, request, relevantHistory);
            
            // Store conversation in session
            var userMessage = new GroupChatMessage
            {
                Content = request.Message,
                Agent = "user",
                Timestamp = DateTime.UtcNow,
                Turn = conversationHistory.Count,
                MessageId = Guid.NewGuid().ToString()
            };
            
            var agentMessage = new GroupChatMessage
            {
                Content = response.Content,
                Agent = response.Agent,
                Timestamp = response.Timestamp,
                Turn = conversationHistory.Count + 1,
                MessageId = Guid.NewGuid().ToString()
            };

            await _sessionManager.AddMessageToSessionAsync(sessionId, userMessage);
            await _sessionManager.AddMessageToSessionAsync(sessionId, agentMessage);

            // Return frontend-compatible single chat response
            return Ok(new
            {
                content = response.Content,
                agent = response.Agent,
                session_id = sessionId,
                timestamp = DateTime.UtcNow.ToString("O"),
                metadata = new { 
                    usage = response.Usage,
                    processing_time_ms = response.ProcessingTimeMs,
                    conversation_length = conversationHistory.Count + 2, // +2 for new user and agent messages
                    history_used = relevantHistory.Count
                }
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Agent not found: {AgentName}", request.Agent);
            return NotFound(new { detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat");
            return StatusCode(500, new { detail = "Internal server error during chat" });
        }
    }
}