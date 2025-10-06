using DotNetAgentFramework.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetAgentFramework.Controllers;

[ApiController]
[Produces("application/json")]
public class GroupChatController(
    IGroupChatService groupChatService,
    ISessionManager sessionManager,
    IAgentService agentService,
    ILogger<GroupChatController> logger) : ControllerBase
{
    private readonly IGroupChatService _groupChatService = groupChatService;
    private readonly ISessionManager _sessionManager = sessionManager;
    private readonly IAgentService _agentService = agentService;
    private readonly ILogger<GroupChatController> _logger = logger;

    /// <summary>
    /// Start a group chat with multiple agents using Microsoft Agent Framework
    /// Frontend payload: { message, session_id?, config?, summarize?, mode?, agents?, max_turns? }
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

            // Validate and adjust max_turns for better performance
            if (request.MaxTurns <= 0) request.MaxTurns = 2;
            if (request.MaxTurns > 5) 
            {
                _logger.LogWarning("Max turns capped at 5 for performance. Requested: {MaxTurns}", request.MaxTurns);
                request.MaxTurns = 5;
            }

            _logger.LogInformation("Group chat request: Message='{Message}', Agents={Agents}, SessionId='{SessionId}', MaxTurns={MaxTurns}", 
                request.Message, 
                request.Agents != null ? $"[{string.Join(", ", request.Agents)}]" : "null", 
                request.SessionId ?? "null",
                request.MaxTurns);

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

            // Adjust max_turns based on agent count for optimal performance
            var optimalMaxTurns = Math.Max(1, Math.Min(request.MaxTurns, 
                request.Agents.Count > 3 ? 1 : request.Agents.Count > 2 ? 2 : 3));
            
            if (optimalMaxTurns != request.MaxTurns)
            {
                _logger.LogInformation("Adjusting max_turns from {Original} to {Optimal} based on {AgentCount} agents", 
                    request.MaxTurns, optimalMaxTurns, request.Agents.Count);
                request.MaxTurns = optimalMaxTurns;
            }

            _logger.LogInformation("Starting Agent Framework group chat with {AgentCount} agents: {Agents}, MaxTurns: {MaxTurns}", 
                request.Agents.Count, string.Join(", ", request.Agents), request.MaxTurns);

            GroupChatResponse response;
            try
            {
                response = await _groupChatService.StartGroupChatAsync(request);
            }
            catch (TimeoutException tex)
            {
                _logger.LogWarning(tex, "Group chat timed out");
                return StatusCode(408, new { 
                    detail = "Request timed out. Try reducing the number of agents or max_turns.",
                    error_type = "timeout",
                    suggestion = "Use fewer agents (1-2) or reduce max_turns to 1-2 for faster responses."
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Group chat was cancelled");
                return StatusCode(408, new { 
                    detail = "Request was cancelled due to timeout.",
                    error_type = "cancelled",
                    suggestion = "Try with fewer agents or reduce max_turns for faster processing."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Agent Framework group chat service");
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
                    is_terminated = m.IsTerminated,
                    metadata = new { 
                        turn = m.Turn, 
                        agent_type = m.AgentType, 
                        timestamp = m.Timestamp.ToString("O"),
                        terminated = m.IsTerminated
                    }
                }).ToList(),
                summary = response.Summary,
                content = response.Summary ?? responseMessages.LastOrDefault()?.Content,
                metadata = new { 
                    group_chat_type = response.GroupChatType,
                    agent_count = response.AgentCount,
                    agents_used = request.Agents,
                    max_turns_used = request.MaxTurns,
                    agent_framework = true,
                    early_termination = response.TotalTurns < request.MaxTurns * request.Agents.Count,
                    terminated_agents = response.TerminatedAgents ?? new List<string>(),
                    timeout_protection = "enabled"
                }
            };

            _logger.LogInformation("Agent Framework group chat completed successfully with {ResponseCount} responses, early termination: {EarlyTermination}, terminated agents: {TerminatedAgents}", 
                responseMessages.Count, 
                response.TotalTurns < request.MaxTurns * request.Agents.Count,
                string.Join(", ", response.TerminatedAgents ?? new List<string>()));
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