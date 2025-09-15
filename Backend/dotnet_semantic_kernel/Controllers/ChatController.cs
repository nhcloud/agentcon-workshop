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
    private readonly ILogger<ChatController> _logger;

    public ChatController(IAgentService agentService, ILogger<ChatController> logger)
    {
        _agentService = agentService;
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

            _logger.LogInformation("Chat request for agent {AgentName}: {Message}", agentName, request.Message);

            var response = await _agentService.ChatWithAgentAsync(agentName, request);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Agent not found: {AgentName}", agentName);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat with agent {AgentName}", agentName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Generic chat endpoint that routes to the specified agent
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

            var agentName = request.Agent ?? "technical_advisor"; // Default agent
            
            _logger.LogInformation("Generic chat request routed to agent {AgentName}: {Message}", agentName, request.Message);

            var response = await _agentService.ChatWithAgentAsync(agentName, request);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Agent not found: {AgentName}", request.Agent);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in generic chat");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}