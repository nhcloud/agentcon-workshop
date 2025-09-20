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
    /// Returns agents with provider information and counts
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> GetAgents()
    {
        try
        {
            var agents = await _agentService.GetAvailableAgentsAsync();
            var agentList = agents.Select(a => new
            {
                name = a.Name,
                type = GetAgentType(a.Name),
                available = true,
                capabilities = a.Capabilities ?? new List<string>(),
                provider = GetProviderType(a.AgentType, a.Name)
            }).ToList();
            
            _logger.LogInformation("Retrieved {AgentCount} available agents", agentList.Count);
            
            return Ok(new { 
                agents = agentList,
                total = agentList.Count,
                available = agentList.Count(a => a.available)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agents");
            return StatusCode(500, new { detail = "Internal server error while retrieving agents" });
        }
    }

    private static string GetAgentType(string agentName)
    {
        return agentName switch
        {
            "generic_agent" => "generic",
            "people_lookup" => "people_lookup", 
            "knowledge_finder" => "knowledge_finder",
            _ => agentName.Replace("foundry_", "")
        };
    }

    private static string GetProviderType(string agentType, string agentName)
    {
        if (agentName.StartsWith("foundry_") || agentType == "Azure AI Foundry")
        {
            return "azure_foundry";
        }
        return "azure_openai";
    }
}