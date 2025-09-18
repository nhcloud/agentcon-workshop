using DotNetSemanticKernel.Models;
using DotNetSemanticKernel.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetSemanticKernel.Controllers;

[ApiController]
[Route("[controller]")]
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
    /// Frontend expects: { agents: [{ id, name, description, ... }] }
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> GetAgents()
    {
        try
        {
            var agents = await _agentService.GetAvailableAgentsAsync();
            var agentList = agents.Select(a => new
            {
                id = a.Name, // Frontend expects 'id' field
                name = a.Name,
                description = a.Description,
                type = a.AgentType,
                available = true,
                capabilities = a.Capabilities ?? new List<string>()
            }).ToList();
            
            _logger.LogInformation("Retrieved {AgentCount} available agents", agentList.Count);
            
            return Ok(new { 
                agents = agentList
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agents");
            return StatusCode(500, new { detail = "Internal server error while retrieving agents" });
        }
    }
}