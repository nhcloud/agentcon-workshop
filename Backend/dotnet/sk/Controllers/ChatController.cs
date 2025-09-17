using DotNetSemanticKernel.Models;
using DotNetSemanticKernel.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetSemanticKernel.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IAgentService agentService, 
        ISessionManager sessionManager,
        ILogger<ChatController> logger)
    {
        _agentService = agentService;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    /// <summary>
    /// Chat with a specific agent
    /// </summary>
    [HttpPost("{agentName}")]
    public async Task<ActionResult<ChatResponse>> ChatWithAgent(string agentName, [FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required" });
            }

            if (string.IsNullOrWhiteSpace(agentName))
            {
                return BadRequest(new { error = "Agent name is required" });
            }

            _logger.LogInformation("Chat request for agent {AgentName}: {Message}", agentName, request.Message);

            var response = await _agentService.ChatWithAgentAsync(agentName, request);
            
            // Store the conversation in session if session_id is provided
            if (!string.IsNullOrEmpty(request.SessionId))
            {
                var userMessage = new GroupChatMessage
                {
                    Content = request.Message,
                    Agent = "user",
                    Timestamp = DateTime.UtcNow,
                    Turn = 0
                };
                
                var agentMessage = new GroupChatMessage
                {
                    Content = response.Content,
                    Agent = response.Agent,
                    Timestamp = response.Timestamp,
                    Turn = 1
                };

                await _sessionManager.AddMessageToSessionAsync(request.SessionId, userMessage);
                await _sessionManager.AddMessageToSessionAsync(request.SessionId, agentMessage);
            }

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Agent not found: {AgentName}", agentName);
            
            var availableAgents = await _agentService.GetAvailableAgentsAsync();
            return NotFound(new { 
                error = ex.Message,
                agent_requested = agentName,
                available_agents = availableAgents.Select(a => a.Name).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat with agent {AgentName}", agentName);
            return StatusCode(500, new { error = "Internal server error during chat" });
        }
    }

    /// <summary>
    /// Generic chat endpoint that routes to the specified agent or default agent
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required" });
            }

            var agentName = request.Agent ?? "generic"; // Default to generic agent
            
            _logger.LogInformation("Generic chat request routed to agent {AgentName}: {Message}", agentName, request.Message);

            var response = await _agentService.ChatWithAgentAsync(agentName, request);
            
            // Store the conversation in session if session_id is provided or create new session
            var sessionId = request.SessionId ?? await _sessionManager.CreateSessionAsync();
            response.SessionId = sessionId;

            var userMessage = new GroupChatMessage
            {
                Content = request.Message,
                Agent = "user",
                Timestamp = DateTime.UtcNow,
                Turn = 0
            };
            
            var agentMessage = new GroupChatMessage
            {
                Content = response.Content,
                Agent = response.Agent,
                Timestamp = response.Timestamp,
                Turn = 1
            };

            await _sessionManager.AddMessageToSessionAsync(sessionId, userMessage);
            await _sessionManager.AddMessageToSessionAsync(sessionId, agentMessage);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Agent not found: {AgentName}", request.Agent);
            
            var availableAgents = await _agentService.GetAvailableAgentsAsync();
            return NotFound(new { 
                error = ex.Message,
                agent_requested = request.Agent,
                available_agents = availableAgents.Select(a => a.Name).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in generic chat");
            return StatusCode(500, new { error = "Internal server error during generic chat" });
        }
    }

    /// <summary>
    /// Start a new chat session
    /// </summary>
    [HttpPost("session/new")]
    public async Task<ActionResult<object>> CreateNewSession()
    {
        try
        {
            var sessionId = await _sessionManager.CreateSessionAsync();
            _logger.LogInformation("Created new chat session: {SessionId}", sessionId);
            
            return Ok(new {
                session_id = sessionId,
                created_at = DateTime.UtcNow,
                message = "New chat session created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new chat session");
            return StatusCode(500, new { error = "Internal server error while creating session" });
        }
    }

    /// <summary>
    /// Continue a conversation in an existing session
    /// </summary>
    [HttpPost("session/{sessionId}/continue")]
    public async Task<ActionResult<ChatResponse>> ContinueSession(string sessionId, [FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required" });
            }

            var sessionExists = await _sessionManager.SessionExistsAsync(sessionId);
            if (!sessionExists)
            {
                return NotFound(new { error = "Session not found", session_id = sessionId });
            }

            // Get session history for context
            var history = await _sessionManager.GetSessionHistoryAsync(sessionId);
            var conversationContext = BuildConversationContext(history);

            // Use the last agent from the session or default to generic
            var lastAgentMessage = history.LastOrDefault(m => m.Agent != "user");
            var agentName = request.Agent ?? lastAgentMessage?.Agent ?? "generic";

            // Add context to the request
            request.SessionId = sessionId;
            request.Context = conversationContext;

            var response = await _agentService.ChatWithAgentAsync(agentName, request);
            response.SessionId = sessionId;

            // Store new messages
            var userMessage = new GroupChatMessage
            {
                Content = request.Message,
                Agent = "user",
                Timestamp = DateTime.UtcNow,
                Turn = history.Count
            };
            
            var agentMessage = new GroupChatMessage
            {
                Content = response.Content,
                Agent = response.Agent,
                Timestamp = response.Timestamp,
                Turn = history.Count + 1
            };

            await _sessionManager.AddMessageToSessionAsync(sessionId, userMessage);
            await _sessionManager.AddMessageToSessionAsync(sessionId, agentMessage);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Agent not found for session continuation");
            return BadRequest(new { error = ex.Message, session_id = sessionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error continuing session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal server error while continuing session" });
        }
    }

    private string BuildConversationContext(List<GroupChatMessage> history)
    {
        if (!history.Any()) return "";

        var recentMessages = history.TakeLast(6).ToList();
        var contextParts = recentMessages.Select(m => $"{m.Agent}: {m.Content}").ToList();
        
        return $"Recent conversation context:\n{string.Join("\n", contextParts)}";
    }
}