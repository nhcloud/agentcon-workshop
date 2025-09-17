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
    /// Get all available agents including Azure AI Foundry agents
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AgentInfo>>> GetAgents()
    {
        try
        {
            var agents = await _agentService.GetAvailableAgentsAsync();
            var agentList = agents.ToList();
            
            _logger.LogInformation("Retrieved {AgentCount} available agents", agentList.Count);
            
            return Ok(new { 
                agent_count = agentList.Count,
                agents = agentList
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agents");
            return StatusCode(500, new { error = "Internal server error while retrieving agents" });
        }
    }

    /// <summary>
    /// Get available agent types/categories
    /// </summary>
    [HttpGet("types")]
    public async Task<ActionResult<object>> GetAgentTypes()
    {
        try
        {
            var agents = await _agentService.GetAvailableAgentsAsync();
            var agentsByType = agents.GroupBy(a => a.AgentType)
                                   .ToDictionary(g => g.Key, g => g.Select(a => new { a.Name, a.Description }).ToList());

            return Ok(new {
                agent_types = agentsByType,
                total_agents = agents.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agent types");
            return StatusCode(500, new { error = "Internal server error while retrieving agent types" });
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
            if (string.IsNullOrWhiteSpace(agentName))
            {
                return BadRequest(new { error = "Agent name is required" });
            }

            var agent = await _agentService.GetAgentAsync(agentName);
            if (agent == null)
            {
                var availableAgents = await _agentService.GetAvailableAgentsAsync();
                return NotFound(new { 
                    error = $"Agent '{agentName}' not found",
                    available_agents = availableAgents.Select(a => a.Name).ToList()
                });
            }

            var agentInfo = new AgentInfo
            {
                Name = agent.Name,
                Description = agent.Description,
                Instructions = agent.Instructions,
                Model = agent.Name.StartsWith("foundry_") ? "Azure AI Foundry" : "Azure OpenAI - GPT-4o",
                AgentType = agent.Name.StartsWith("foundry_") ? "Azure AI Foundry" : "Standard"
            };

            return Ok(agentInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agent {AgentName}", agentName);
            return StatusCode(500, new { error = "Internal server error while retrieving agent" });
        }
    }

    /// <summary>
    /// Chat with a specific agent
    /// </summary>
    [HttpPost("{agentName}/chat")]
    public async Task<ActionResult<ChatResponse>> ChatWithAgent(string agentName, [FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(agentName))
            {
                return BadRequest(new { error = "Agent name is required" });
            }

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
            _logger.LogWarning(ex, "Invalid argument for agent chat: {AgentName}", agentName);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat with agent {AgentName}", agentName);
            return StatusCode(500, new { error = "Internal server error during agent chat" });
        }
    }

    /// <summary>
    /// Create and test an Azure AI Foundry agent
    /// </summary>
    [HttpPost("foundry/{agentType}/test")]
    public async Task<ActionResult<object>> TestAzureFoundryAgent(string agentType, [FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(agentType))
            {
                return BadRequest(new { error = "Agent type is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required for testing" });
            }

            var foundryAgent = await _agentService.CreateAzureFoundryAgentAsync(agentType);
            if (foundryAgent == null)
            {
                return BadRequest(new { 
                    error = "Azure AI Foundry agent could not be created",
                    agent_type = agentType,
                    possible_reasons = new[] {
                        "Azure AI Foundry not configured",
                        "Agent type not supported",
                        "Missing agent ID in configuration"
                    }
                });
            }

            var response = await foundryAgent.ChatAsync(request);
            
            return Ok(new {
                agent_type = "Azure AI Foundry",
                agent_name = foundryAgent.Name,
                test_successful = true,
                response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Azure AI Foundry agent {AgentType}", agentType);
            return StatusCode(500, new { 
                error = "Internal server error while testing Azure AI Foundry agent",
                agent_type = agentType
            });
        }
    }

    /// <summary>
    /// Get agent capabilities and features
    /// </summary>
    [HttpGet("{agentName}/capabilities")]
    public async Task<ActionResult<object>> GetAgentCapabilities(string agentName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(agentName))
            {
                return BadRequest(new { error = "Agent name is required" });
            }

            var agent = await _agentService.GetAgentAsync(agentName);
            if (agent == null)
            {
                return NotFound(new { error = $"Agent '{agentName}' not found" });
            }

            var capabilities = new List<string>();
            var features = new List<string>();

            // Determine capabilities based on agent type
            if (agent.Name.StartsWith("foundry_"))
            {
                capabilities.AddRange(new[] {
                    "Enterprise-grade security",
                    "Azure AD integration", 
                    "Managed identity support",
                    "Advanced monitoring"
                });
                features.AddRange(new[] {
                    "Azure AI Foundry integration",
                    "Pre-configured enterprise agents",
                    "Advanced compliance features"
                });
            }
            else
            {
                capabilities.AddRange(new[] {
                    "Natural language processing",
                    "Context awareness",
                    "Multi-turn conversations",
                    "Specialized domain knowledge"
                });
                features.AddRange(new[] {
                    "Semantic Kernel integration", 
                    "Azure OpenAI powered",
                    "Customizable instructions"
                });
            }

            // Add agent-specific capabilities
            switch (agent.Name.Replace("foundry_", ""))
            {
                case "people_lookup":
                    capabilities.AddRange(new[] { "People directory search", "Contact information retrieval", "Organizational structure navigation" });
                    break;
                case "knowledge_finder":
                    capabilities.AddRange(new[] { "Document search", "Information synthesis", "Knowledge base querying" });
                    break;
                case "task_assistant":
                    capabilities.AddRange(new[] { "Task breakdown", "Project planning", "Productivity optimization" });
                    break;
                case "technical_advisor":
                    capabilities.AddRange(new[] { "Code review", "Architecture guidance", "Technical decision support" });
                    break;
                case "creative_assistant":
                    capabilities.AddRange(new[] { "Content creation", "Creative ideation", "Storytelling" });
                    break;
            }

            return Ok(new {
                agent_name = agent.Name,
                agent_type = agent.Name.StartsWith("foundry_") ? "Azure AI Foundry" : "Standard",
                capabilities,
                features,
                description = agent.Description
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving capabilities for agent {AgentName}", agentName);
            return StatusCode(500, new { error = "Internal server error while retrieving agent capabilities" });
        }
    }
}