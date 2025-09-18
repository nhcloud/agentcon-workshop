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
    /// Frontend payload: { message, session_id?, config?, summarize?, mode?, agents? }
    /// If no agents provided, will auto-select appropriate agents based on the message
    /// </summary>
    [HttpPost("group-chat")]
    public async Task<ActionResult<object>> StartGroupChat([FromBody] GroupChatRequest request)
    {
        try
        {
            // Log raw request for debugging
            _logger.LogInformation("Group chat endpoint called");
            
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { detail = "Message is required" });
            }

            // Enhanced logging for debugging
            _logger.LogInformation("Group chat request: Message='{Message}', Agents={Agents}, SessionId='{SessionId}', Mode='{Mode}', Config={Config}", 
                request.Message, 
                request.Agents != null ? $"[{string.Join(", ", request.Agents)}]" : "null", 
                request.SessionId ?? "null",
                request.Mode,
                request.Config?.ToString() ?? "null");

            // Auto-select agents if none provided (matching Python backend behavior)
            if (request.Agents == null || !request.Agents.Any())
            {
                _logger.LogInformation("No agents specified, auto-selecting agents for group chat");
                
                // Get available agents
                var availableAgents = await _agentService.GetAvailableAgentsAsync();
                var availableAgentsList = availableAgents.ToList();
                
                _logger.LogInformation("Available agents: {AvailableAgents}", 
                    string.Join(", ", availableAgentsList.Select(a => a.Name)));

                // Smart agent selection based on available agents
                var selectedAgents = new List<string>();

                // Prefer specialized agents for group chat
                var specializedAgents = availableAgentsList
                    .Where(a => a.Name != "generic_agent" && a.Name != "generic")
                    .OrderBy(a => a.Name) // Consistent ordering
                    .Take(2)
                    .Select(a => a.Name)
                    .ToList();

                selectedAgents.AddRange(specializedAgents);

                // If we don't have enough agents, add generic agent
                if (selectedAgents.Count == 0)
                {
                    var genericAgent = availableAgentsList.FirstOrDefault(a => a.Name == "generic_agent" || a.Name == "generic");
                    if (genericAgent != null)
                    {
                        selectedAgents.Add(genericAgent.Name);
                    }
                }

                // Ensure we have at least one agent (fallback)
                if (!selectedAgents.Any())
                {
                    selectedAgents.Add("generic_agent");
                }

                request.Agents = selectedAgents;
                _logger.LogInformation("Auto-selected agents for group chat: {Agents}", string.Join(", ", request.Agents));
            }

            // Validate that the selected agents exist
            var availableAgentsForValidation = await _agentService.GetAvailableAgentsAsync();
            var availableAgentNames = availableAgentsForValidation.Select(a => a.Name).ToHashSet();
            var invalidAgents = request.Agents.Where(a => !availableAgentNames.Contains(a)).ToList();
            
            if (invalidAgents.Any())
            {
                _logger.LogWarning("Invalid agents specified: {InvalidAgents}", string.Join(", ", invalidAgents));
                return BadRequest(new { 
                    detail = "Invalid agents specified", 
                    invalid_agents = invalidAgents,
                    available_agents = availableAgentNames.ToList(),
                    suggestion = "Use /agents endpoint to get list of available agents"
                });
            }

            _logger.LogInformation("Starting group chat with {AgentCount} agents: {Agents}", 
                request.Agents.Count, string.Join(", ", request.Agents));

            // Use Semantic Kernel AgentGroupChat if requested, otherwise standard
            GroupChatResponse response;
            try
            {
                response = request.UseSemanticKernelGroupChat 
                    ? await _groupChatService.StartSemanticKernelGroupChatAsync(request)
                    : await _groupChatService.StartGroupChatAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in group chat service");
                return StatusCode(500, new { detail = $"Group chat service error: {ex.Message}" });
            }

            // Transform response to match frontend expectations exactly
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
                    auto_selected = true
                }
            };

            _logger.LogInformation("Group chat completed successfully with {ResponseCount} responses", responseMessages.Count);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in group chat request");
            return BadRequest(new { detail = ex.Message });
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