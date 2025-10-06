using DotNetAgentFramework.Models;
using Microsoft.Extensions.Configuration;

namespace DotNetAgentFramework.Services;

/// <summary>
/// Service for managing group chat templates from YAML configuration
/// </summary>
public class GroupChatTemplateService : IGroupChatTemplateService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GroupChatTemplateService> _logger;
    private readonly Dictionary<string, object> _templates;

    public GroupChatTemplateService(IConfiguration configuration, ILogger<GroupChatTemplateService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _templates = LoadTemplatesFromConfig();
    }

    private Dictionary<string, object> LoadTemplatesFromConfig()
    {
        try
        {
            var groupChatsSection = _configuration.GetSection("group_chats:templates");
            var templates = new Dictionary<string, object>();
            
            foreach (var template in groupChatsSection.GetChildren())
            {
                var templateData = new Dictionary<string, object>();
                
                // Load template properties
                templateData["name"] = template["name"] ?? template.Key;
                templateData["description"] = template["description"] ?? "";
                templateData["max_turns"] = int.TryParse(template["max_turns"], out var maxTurns) ? maxTurns : 6;
                templateData["auto_select_speaker"] = bool.TryParse(template["auto_select_speaker"], out var autoSelect) && autoSelect;
                
                // Load participants
                var participants = new List<Dictionary<string, object>>();
                var participantsSection = template.GetSection("participants");
                
                foreach (var participant in participantsSection.GetChildren())
                {
                    var participantData = new Dictionary<string, object>
                    {
                        ["name"] = participant["name"] ?? "",
                        ["instructions"] = participant["instructions"] ?? "",
                        ["role"] = participant["role"] ?? "participant",
                        ["priority"] = int.TryParse(participant["priority"], out var priority) ? priority : 1,
                        ["max_consecutive_turns"] = int.TryParse(participant["max_consecutive_turns"], out var maxConsecutive) ? maxConsecutive : 3
                    };
                    participants.Add(participantData);
                }
                
                templateData["participants"] = participants;
                templates[template.Key] = templateData;
            }
            
            _logger.LogInformation("Loaded {TemplateCount} group chat templates from configuration", templates.Count);
            return templates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading group chat templates from configuration");
            return new Dictionary<string, object>();
        }
    }

    public async Task<IEnumerable<GroupChatTemplateInfo>> GetAvailableTemplatesAsync()
    {
        return await Task.FromResult(_templates.Select(kvp =>
        {
            var templateData = (Dictionary<string, object>)kvp.Value;
            var participants = (List<Dictionary<string, object>>)templateData["participants"];
            
            return new GroupChatTemplateInfo
            {
                Id = kvp.Key,
                Name = templateData["name"].ToString() ?? kvp.Key,
                Description = templateData["description"].ToString() ?? "",
                MaxTurns = (int)templateData["max_turns"],
                AutoSelectSpeaker = (bool)templateData["auto_select_speaker"],
                ParticipantsCount = participants.Count,
                Participants = participants.Select(p => new GroupChatParticipantInfo
                {
                    Name = p["name"].ToString() ?? "",
                    Role = p["role"].ToString() ?? "participant",
                    Priority = (int)p["priority"]
                }).ToList()
            };
        }).ToList());
    }

    public async Task<GroupChatTemplateDetails?> GetTemplateDetailsAsync(string templateName)
    {
        if (!_templates.TryGetValue(templateName, out var templateObj))
        {
            return null;
        }

        var templateData = (Dictionary<string, object>)templateObj;
        var participants = (List<Dictionary<string, object>>)templateData["participants"];

        return await Task.FromResult(new GroupChatTemplateDetails
        {
            Id = templateName,
            Name = templateData["name"].ToString() ?? templateName,
            Description = templateData["description"].ToString() ?? "",
            MaxTurns = (int)templateData["max_turns"],
            AutoSelectSpeaker = (bool)templateData["auto_select_speaker"],
            ParticipantsCount = participants.Count,
            Participants = participants.Select(p => new GroupChatParticipantInfo
            {
                Name = p["name"].ToString() ?? "",
                Role = p["role"].ToString() ?? "participant",
                Priority = (int)p["priority"]
            }).ToList(),
            ParticipantsDetail = participants.Select(p => new GroupChatParticipantDetails
            {
                Name = p["name"].ToString() ?? "",
                Instructions = p["instructions"].ToString() ?? "",
                Role = p["role"].ToString() ?? "participant",
                Priority = (int)p["priority"],
                MaxConsecutiveTurns = (int)p["max_consecutive_turns"]
            }).ToList()
        });
    }

    public async Task<GroupChatRequest?> CreateFromTemplateAsync(string templateName)
    {
        var templateDetails = await GetTemplateDetailsAsync(templateName);
        if (templateDetails == null)
        {
            return null;
        }

        return new GroupChatRequest
        {
            Message = "", // Will be set by caller
            Agents = templateDetails.ParticipantsDetail?.Select(p => p.Name).ToList() ?? new List<string>(),
            MaxTurns = templateDetails.MaxTurns,
            Config = new Dictionary<string, object>
            {
                ["name"] = templateDetails.Name,
                ["description"] = templateDetails.Description,
                ["auto_select_speaker"] = templateDetails.AutoSelectSpeaker,
                ["template_name"] = templateName
            }
        };
    }
}