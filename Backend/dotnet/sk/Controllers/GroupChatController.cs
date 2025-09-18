using DotNetSemanticKernel.Models;
using DotNetSemanticKernel.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetSemanticKernel.Controllers;

[ApiController]
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
    /// Matches Python: POST /group-chat
    /// </summary>
    [HttpPost("group-chat")]
    public async Task<ActionResult<object>> StartGroupChat([FromBody] GroupChatRequest request)
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

            // Use Semantic Kernel AgentGroupChat if requested, otherwise standard
            var response = request.UseSemanticKernelGroupChat 
                ? await _groupChatService.StartSemanticKernelGroupChatAsync(request)
                : await _groupChatService.StartGroupChatAsync(request);

            // Transform response to match Python FastAPI format
            var responseMessages = response.Messages?.Where(m => m.Agent != "user").ToList() ?? new List<GroupChatMessage>();
            
            return Ok(new
            {
                conversation_id = response.SessionId,
                total_turns = response.TotalTurns,
                active_participants = response.Messages?.Select(m => m.Agent).Distinct().Where(a => a != "user").ToList() ?? new List<string>(),
                responses = responseMessages.Select(m => new
                {
                    agent = m.Agent,
                    content = m.Content,
                    message_id = m.MessageId,
                    metadata = new { turn = m.Turn, agent_type = m.AgentType, timestamp = m.Timestamp }
                }).ToList(),
                summary = response.Summary,
                content = response.Summary ?? responseMessages.LastOrDefault()?.Content,
                metadata = new { 
                    group_chat_type = response.GroupChatType,
                    agent_count = response.AgentCount
                }
            });
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
    /// Get available group chat templates
    /// Matches Python: GET /group-chat/templates
    /// </summary>
    [HttpGet("group-chat/templates")]
    public ActionResult<object> GetGroupChatTemplates()
    {
        try
        {
            // Return empty templates for now - can be extended later
            return Ok(new { 
                templates = new List<object>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving group chat templates");
            return StatusCode(500, new { error = "Internal server error while retrieving templates" });
        }
    }

    /// <summary>
    /// Get active group chats
    /// Matches Python: GET /group-chats
    /// </summary>
    [HttpGet("group-chats")]
    public async Task<ActionResult<object>> GetActiveGroupChats()
    {
        try
        {
            var activeSessions = await _sessionManager.GetActiveSessionsAsync();
            var groupChats = new List<object>();
            
            foreach (var sessionId in activeSessions)
            {
                try
                {
                    var sessionInfo = await _sessionManager.GetSessionInfoAsync(sessionId);
                    groupChats.Add(new
                    {
                        session_id = sessionId,
                        created_at = sessionInfo.CreatedAt,
                        last_activity = sessionInfo.LastActivity,
                        message_count = sessionInfo.MessageCount,
                        agent_types = sessionInfo.AgentTypes
                    });
                }
                catch
                {
                    // Skip invalid sessions
                }
            }
            
            return Ok(new { 
                group_chats = groupChats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active group chats");
            return StatusCode(500, new { error = "Internal server error while retrieving group chats" });
        }
    }
}