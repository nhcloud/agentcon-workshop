using DotNetAgentFramework.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetAgentFramework.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class ChatController(
    IAgentService agentService,
    ISessionManager sessionManager,
    IGroupChatService groupChatService,
    IGroupChatTemplateService templateService,
    ILogger<ChatController> logger) : ControllerBase
{
    private readonly IAgentService _agentService = agentService;
    private readonly ISessionManager _sessionManager = sessionManager;
    private readonly IGroupChatService _groupChatService = groupChatService;
    private readonly IGroupChatTemplateService _templateService = templateService;
    private readonly ILogger<ChatController> _logger = logger;

    /// <summary>
    /// Process a chat message - handles both single and multiple agents using Microsoft Agent Framework
    /// Frontend payload: { message, session_id?, agents? }
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<object>> Chat([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { detail = "Message is required" });
            }

            // Generate session ID if not provided (matching frontend expectation)
            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();

            // Retrieve conversation history for the session
            List<GroupChatMessage> conversationHistory = new();
            if (!string.IsNullOrEmpty(request.SessionId))
            {
                try
                {
                    conversationHistory = await _sessionManager.GetSessionHistoryAsync(request.SessionId);
                    _logger.LogInformation("Retrieved {MessageCount} messages from session {SessionId}", conversationHistory.Count, request.SessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve session history for {SessionId}, starting fresh", request.SessionId);
                    conversationHistory = new List<GroupChatMessage>();
                }
            }

            // Check if multiple agents were specified (frontend sends agents array)
            if (request.Agents != null && request.Agents.Count > 1)
            {
                // Route to group chat for multiple agents using Agent Framework
                var groupRequest = new GroupChatRequest
                {
                    Message = request.Message,
                    Agents = request.Agents,
                    SessionId = sessionId,
                    MaxTurns = 1
                };

                var groupResponse = await _groupChatService.StartGroupChatAsync(groupRequest);
                var responseMessages = groupResponse.Messages?.Where(m => m.Agent != "user").ToList() ?? new List<GroupChatMessage>();
                var lastMessage = responseMessages.LastOrDefault();

                // Return frontend-compatible group chat response
                return Ok(new
                {
                    content = lastMessage?.Content ?? "No response generated",
                    agent = lastMessage?.Agent ?? "system",
                    session_id = sessionId,
                    timestamp = DateTime.UtcNow.ToString("O"),
                    metadata = new { 
                        total_agents = request.Agents.Count,
                        group_chat = true,
                        agent_framework = true,
                        all_responses = responseMessages.Select(m => new { agent = m.Agent, content = m.Content }).ToList(),
                        conversation_length = conversationHistory.Count
                    }
                });
            }

            // Single agent handling with conversation history
            var agentName = request.Agents?.FirstOrDefault() ?? "generic_agent";
            
            _logger.LogInformation("Chat request for agent {AgentName} with {HistoryCount} previous messages: {Message}", 
                agentName, conversationHistory.Count, request.Message);

            // Filter conversation history to only include relevant messages for this agent
            var relevantHistory = conversationHistory
                .Where(m => m.Agent == "user" || m.Agent == agentName)
                .ToList();

            var response = await _agentService.ChatWithAgentAsync(agentName, request, relevantHistory);
            
            // Store conversation in session
            var userMessage = new GroupChatMessage
            {
                Content = request.Message,
                Agent = "user",
                Timestamp = DateTime.UtcNow,
                Turn = conversationHistory.Count,
                MessageId = Guid.NewGuid().ToString()
            };
            
            var agentMessage = new GroupChatMessage
            {
                Content = response.Content,
                Agent = response.Agent,
                Timestamp = response.Timestamp,
                Turn = conversationHistory.Count + 1,
                MessageId = Guid.NewGuid().ToString()
            };

            await _sessionManager.AddMessageToSessionAsync(sessionId, userMessage);
            await _sessionManager.AddMessageToSessionAsync(sessionId, agentMessage);

            // Return frontend-compatible single chat response
            return Ok(new
            {
                content = response.Content,
                agent = response.Agent,
                session_id = sessionId,
                timestamp = DateTime.UtcNow.ToString("O"),
                metadata = new { 
                    usage = response.Usage,
                    processing_time_ms = response.ProcessingTimeMs,
                    conversation_length = conversationHistory.Count + 2, // +2 for new user and agent messages
                    history_used = relevantHistory.Count,
                    agent_framework = true
                }
            });
        }
        catch (ArgumentException ex)
        {
            var agentName = request.Agents?.FirstOrDefault() ?? "unknown";
            _logger.LogWarning(ex, "Agent not found: {AgentName}", agentName);
            return NotFound(new { detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat");
            return StatusCode(500, new { detail = "Internal server error during chat" });
        }
    }

    /// <summary>
    /// Get available group chat templates
    /// Frontend expects: { templates: [] }
    /// </summary>
    [HttpGet("group-chat/templates")]
    public async Task<ActionResult<object>> GetGroupChatTemplates()
    {
        try
        {
            var templates = await _templateService.GetAvailableTemplatesAsync();
            
            return Ok(new { 
                templates = templates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving group chat templates");
            return StatusCode(500, new { detail = "Internal server error while retrieving templates" });
        }
    }

    /// <summary>
    /// Get detailed information about a specific template
    /// </summary>
    [HttpGet("group-chat/templates/{templateName}")]
    public async Task<ActionResult<object>> GetGroupChatTemplate(string templateName)
    {
        try
        {
            var templateDetails = await _templateService.GetTemplateDetailsAsync(templateName);
            
            if (templateDetails == null)
            {
                return NotFound(new { detail = $"Template '{templateName}' not found" });
            }
            
            return Ok(templateDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template {TemplateName}", templateName);
            return StatusCode(500, new { detail = "Internal server error while retrieving template" });
        }
    }

    /// <summary>
    /// Create group chat from template
    /// </summary>
    [HttpPost("group-chat/from-template")]
    public async Task<ActionResult<object>> CreateGroupChatFromTemplate([FromBody] CreateFromTemplateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.TemplateName))
            {
                return BadRequest(new { detail = "Template name is required" });
            }

            var templateRequest = await _templateService.CreateFromTemplateAsync(request.TemplateName);
            
            if (templateRequest == null)
            {
                return NotFound(new { detail = $"Template '{request.TemplateName}' not found" });
            }

            var templateDetails = await _templateService.GetTemplateDetailsAsync(request.TemplateName);

            return Ok(new
            {
                session_id = Guid.NewGuid().ToString(),
                template_name = request.TemplateName,
                name = templateDetails?.Name ?? request.TemplateName,
                description = templateDetails?.Description ?? "",
                participants = templateRequest.Agents,
                status = "created",
                config = templateRequest.Config
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group chat from template {TemplateName}", request.TemplateName);
            return StatusCode(500, new { detail = "Internal server error while creating group chat from template" });
        }
    }
}