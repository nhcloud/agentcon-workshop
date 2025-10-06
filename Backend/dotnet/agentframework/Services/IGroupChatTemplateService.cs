using DotNetAgentFramework.Models;

namespace DotNetAgentFramework.Services;

/// <summary>
/// Service interface for managing group chat templates
/// </summary>
public interface IGroupChatTemplateService
{
    /// <summary>
    /// Get all available group chat templates
    /// </summary>
    /// <returns>List of group chat template information</returns>
    Task<IEnumerable<GroupChatTemplateInfo>> GetAvailableTemplatesAsync();
    
    /// <summary>
    /// Get detailed information about a specific template
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <returns>Template details or null if not found</returns>
    Task<GroupChatTemplateDetails?> GetTemplateDetailsAsync(string templateName);
    
    /// <summary>
    /// Create a group chat request from a template
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <returns>Group chat request configured from template</returns>
    Task<GroupChatRequest?> CreateFromTemplateAsync(string templateName);
}