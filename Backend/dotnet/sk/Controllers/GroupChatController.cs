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
    private readonly ILogger<GroupChatController> _logger;

    public GroupChatController(
        IGroupChatService groupChatService, 
        ISessionManager sessionManager,
        ILogger<GroupChatController> logger)
    {
        _groupChatService = groupChatService;
        _sessionManager = sessionManager;
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

            _logger.LogInformation("Group chat request with {AgentCount} agents: {Agents}", 
                request.Agents.Count, string.Join(", ", request.Agents));

            var response = await _groupChatService.StartGroupChatAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in group chat");
            return StatusCode(500, new { error = "Internal server error" });
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
            var history = await _sessionManager.GetSessionHistoryAsync(sessionId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session history for {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal server error" });
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
            await _sessionManager.ClearSessionAsync(sessionId);
            return Ok(new { message = "Session cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal server error" });
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
            var exists = await _sessionManager.SessionExistsAsync(sessionId);
            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking session existence for {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}