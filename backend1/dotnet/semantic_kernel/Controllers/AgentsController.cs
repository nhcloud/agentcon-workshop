using DotNetSemanticKernel.Models;
using DotNetSemanticKernel.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetSemanticKernel.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(IAgentService agentService, ILogger<AgentsController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available agents
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AgentInfo>>> GetAgents()
    {
        try
        {
            var agents = await _agentService.GetAvailableAgentsAsync();
            return Ok(agents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agents");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get specific agent information
    /// </summary>
    [HttpGet("{agentName}")]
    public async Task<ActionResult<AgentInfo>> GetAgent(string agentName)
    {
        try
        {
            var agent = await _agentService.GetAgentAsync(agentName);
            if (agent == null)
            {
                return NotFound(new { error = $"Agent '{agentName}' not found" });
            }

            var agentInfo = new AgentInfo
            {
                Name = agent.Name,
                Description = agent.Description,
                Instructions = agent.Instructions,
                Model = "gpt-4o"
            };

            return Ok(agentInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agent {AgentName}", agentName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}