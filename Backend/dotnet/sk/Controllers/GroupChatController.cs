using DotNetSemanticKernel.Models;
using DotNetSemanticKernel.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetSemanticKernel.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class GroupChatController : ControllerBase
{
    private readonly IGroupChatService _groupChatService;
    private readonly ISessionManager _sessionManager;
    private readonly IAgentService _agentService;
    private readonly ILogger<GroupChatController> _logger;

    public GroupChatController(
        IGroupChatService groupChatService, 
        ISessionManager sessionManager,
        IAgentService agentService,
        ILogger<GroupChatController> logger)
    {
        _groupChatService = groupChatService;
        _sessionManager = sessionManager;
        _agentService = agentService;
        _logger = logger;
    }

    /// <summary>
    /// Start a group chat with multiple agents
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<GroupChatResponse>> StartGroupChat([FromBody] GroupChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required" });
            }

            if (request.Agents == null || !request.Agents.Any())
            {
                return BadRequest(new { error = "At least one agent must be specified" });
            }

            // Validate agents exist
            var availableAgents = await _agentService.GetAvailableAgentsAsync();
            var availableAgentNames = availableAgents.Select(a => a.Name).ToHashSet();
            var invalidAgents = request.Agents.Where(a => !availableAgentNames.Contains(a)).ToList();
            
            if (invalidAgents.Any())
            {
                return BadRequest(new { 
                    error = "Invalid agents specified", 
                    invalid_agents = invalidAgents,
                    available_agents = availableAgentNames.ToList()
                });
            }

            _logger.LogInformation("Group chat request with {AgentCount} agents: {Agents}", 
                request.Agents.Count, string.Join(", ", request.Agents));

            var startTime = DateTime.UtcNow;
            GroupChatResponse response;
            
            // Use Semantic Kernel AgentGroupChat if requested and supported
            if (request.UseSemanticKernelGroupChat)
            {
                _logger.LogInformation("Using Semantic Kernel AgentGroupChat implementation");
                response = await _groupChatService.StartSemanticKernelGroupChatAsync(request);
            }
            else
            {
                _logger.LogInformation("Using standard group chat implementation");
                response = await _groupChatService.StartGroupChatAsync(request);
            }

            var endTime = DateTime.UtcNow;
            response.TotalProcessingTimeMs = (int)(endTime - startTime).TotalMilliseconds;
            response.AgentCount = request.Agents.Count;

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in group chat request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in group chat");
            return StatusCode(500, new { error = "Internal server error occurred during group chat" });
        }
    }

    /// <summary>
    /// Start an advanced Semantic Kernel group chat
    /// </summary>
    [HttpPost("semantic-kernel")]
    public async Task<ActionResult<GroupChatResponse>> StartSemanticKernelGroupChat([FromBody] GroupChatRequest request)
    {
        try
        {
            request.UseSemanticKernelGroupChat = true;
            return await StartGroupChat(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Semantic Kernel group chat");
            return StatusCode(500, new { error = "Internal server error occurred during SK group chat" });
        }
    }

    /// <summary>
    /// Get session history
    /// </summary>
    [HttpGet("sessions/{sessionId}/history")]
    public async Task<ActionResult<IEnumerable<GroupChatMessage>>> GetSessionHistory(string sessionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            var history = await _sessionManager.GetSessionHistoryAsync(sessionId);
            return Ok(new { session_id = sessionId, message_count = history.Count, messages = history });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session history for {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal server error while retrieving session history" });
        }
    }

    /// <summary>
    /// Get session information
    /// </summary>
    [HttpGet("sessions/{sessionId}/info")]
    public async Task<ActionResult<SessionInfo>> GetSessionInfo(string sessionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            var exists = await _sessionManager.SessionExistsAsync(sessionId);
            if (!exists)
            {
                return NotFound(new { error = "Session not found" });
            }

            var history = await _sessionManager.GetSessionHistoryAsync(sessionId);
            var agentTypes = history.Where(m => m.Agent != "user")
                                  .Select(m => m.Agent)
                                  .Distinct()
                                  .ToList();

            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                MessageCount = history.Count,
                AgentTypes = agentTypes,
                CreatedAt = history.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
                LastActivity = history.LastOrDefault()?.Timestamp ?? DateTime.UtcNow
            };

            return Ok(sessionInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session info for {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal server error while retrieving session info" });
        }
    }

    /// <summary>
    /// Clear session history
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    public async Task<ActionResult> ClearSession(string sessionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            await _sessionManager.ClearSessionAsync(sessionId);
            return Ok(new { message = "Session cleared successfully", session_id = sessionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal server error while clearing session" });
        }
    }

    /// <summary>
    /// Check if session exists
    /// </summary>
    [HttpGet("sessions/{sessionId}/exists")]
    public async Task<ActionResult<bool>> SessionExists(string sessionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            var exists = await _sessionManager.SessionExistsAsync(sessionId);
            return Ok(new { session_id = sessionId, exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking session existence for {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal server error while checking session" });
        }
    }

    /// <summary>
    /// Get all active sessions
    /// </summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<IEnumerable<string>>> GetActiveSessions()
    {
        try
        {
            var sessions = await _sessionManager.GetActiveSessionsAsync();
            return Ok(new { session_count = sessions.Count(), sessions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active sessions");
            return StatusCode(500, new { error = "Internal server error while retrieving sessions" });
        }
    }

    /// <summary>
    /// Summarize a conversation from session history
    /// </summary>
    [HttpPost("sessions/{sessionId}/summarize")]
    public async Task<ActionResult<string>> SummarizeSession(string sessionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            var exists = await _sessionManager.SessionExistsAsync(sessionId);
            if (!exists)
            {
                return NotFound(new { error = "Session not found" });
            }

            var history = await _sessionManager.GetSessionHistoryAsync(sessionId);
            if (!history.Any())
            {
                return Ok(new { summary = "No conversation to summarize", session_id = sessionId });
            }

            var summary = await _groupChatService.SummarizeConversationAsync(history);
            return Ok(new { summary, session_id = sessionId, message_count = history.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal server error while summarizing session" });
        }
    }
}