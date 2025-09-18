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
    /// Start a group chat with multiple agents using Semantic Kernel AgentGroupChat
    /// Frontend payload: { message, session_id?, config?, summarize?, mode?, agents? }
    /// Agents can be null - will auto-select available agents
    /// </summary>
    [HttpPost("group-chat")]
    public async Task<ActionResult<object>> StartGroupChat([FromBody] GroupChatRequest request)
    {
        try
        {
            _logger.LogInformation("Group chat endpoint called");
            
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { detail = "Message is required" });
            }

            _logger.LogInformation("Group chat request: Message='{Message}', Agents={Agents}, SessionId='{SessionId}'", 
                request.Message, 
                request.Agents != null ? $"[{string.Join(", ", request.Agents)}]" : "null", 
                request.SessionId ?? "null");

            // Auto-select agents if none provided
            if (request.Agents == null || !request.Agents.Any())
            {
                _logger.LogInformation("No agents specified, auto-selecting all available agents for group chat");
                
                var availableAgents = await _agentService.GetAvailableAgentsAsync();
                var availableAgentsList = availableAgents.ToList();
                
                // Select all available agents for a rich group chat experience
                request.Agents = availableAgentsList.Select(a => a.Name).ToList();
                
                _logger.LogInformation("Auto-selected {AgentCount} agents: {Agents}", 
                    request.Agents.Count, string.Join(", ", request.Agents));
            }
            else
            {
                _logger.LogInformation("Using provided agents: {Agents}", string.Join(", ", request.Agents));
            }

            // Always use Semantic Kernel AgentGroupChat for group chat
            request.UseSemanticKernelGroupChat = true;

            _logger.LogInformation("Starting Semantic Kernel AgentGroupChat with {AgentCount} agents: {Agents}", 
                request.Agents.Count, string.Join(", ", request.Agents));

            GroupChatResponse response;
            try
            {
                response = await _groupChatService.StartSemanticKernelGroupChatAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Semantic Kernel group chat service");
                return StatusCode(500, new { detail = $"Group chat service error: {ex.Message}" });
            }

            // Transform response to match frontend expectations
            var responseMessages = response.Messages?.Where(m => m.Agent != "user").ToList() ?? new List<GroupChatMessage>();
            
            var result = new
            {
                conversation_id = response.SessionId,
                total_turns = response.TotalTurns,
                active_participants = response.Messages?.Select(m => m.Agent).Distinct().Where(a => a != "user").ToList() ?? new List<string>(),
                responses = responseMessages.Select(m => new
                {
                    agent = m.Agent,
                    content = m.Content,
                    message_id = m.MessageId,
                    metadata = new { 
                        turn = m.Turn, 
                        agent_type = m.AgentType, 
                        timestamp = m.Timestamp.ToString("O")
                    }
                }).ToList(),
                summary = response.Summary,
                content = response.Summary ?? responseMessages.LastOrDefault()?.Content,
                metadata = new { 
                    group_chat_type = response.GroupChatType,
                    agent_count = response.AgentCount,
                    agents_used = request.Agents,
                    semantic_kernel_groupchat = true
                }
            };

            _logger.LogInformation("Semantic Kernel group chat completed successfully with {ResponseCount} responses", responseMessages.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in group chat");
            return StatusCode(500, new { detail = "Internal server error occurred during group chat" });
        }
    }

    /// <summary>
    /// Get available group chat templates
    /// Frontend expects: { templates: [] }
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
            return StatusCode(500, new { detail = "Internal server error while retrieving templates" });
        }
    }

    /// <summary>
    /// Get active group chats
    /// Frontend expects: { group_chats: [] }
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
                        created_at = sessionInfo.CreatedAt.ToString("O"),
                        last_activity = sessionInfo.LastActivity.ToString("O"),
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
            return StatusCode(500, new { detail = "Internal server error while retrieving group chats" });
        }
    }
}