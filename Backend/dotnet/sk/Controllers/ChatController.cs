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
    /// Process a chat message
    /// Matches Python: POST /chat
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<object>> Chat([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required" });
            }

            // Generate session ID if not provided (matching Python behavior)
            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
            
            // Use specified agent or default to generic
            var agentName = request.Agent ?? "generic";
            
            _logger.LogInformation("Chat request for agent {AgentName}: {Message}", agentName, request.Message);

            var response = await _agentService.ChatWithAgentAsync(agentName, request);
            
            // Store conversation in session
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

            // Return Python-compatible format
            return Ok(new
            {
                content = response.Content,
                agent = response.Agent,
                usage = response.Usage,
                session_id = sessionId,
                message_id = Guid.NewGuid().ToString(),
                metadata = new { }
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Agent not found: {AgentName}", request.Agent);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat");
            return StatusCode(500, new { error = "Internal server error during chat" });
        }
    }
}