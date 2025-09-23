using System.Text.Json;
using YamlDotNet.Serialization;
using DotNetSemanticKernel.Configuration;

namespace DotNetSemanticKernel.Services;

public class AgentInstructionsService
{
    private readonly Dictionary<string, AgentDefinition> _agents;

    public AgentInstructionsService(IConfiguration configuration)
    {
        _agents = new Dictionary<string, AgentDefinition>();
        
        // Try to load from YAML config
        var agentsSection = configuration.GetSection("agents");
        if (agentsSection.Exists())
        {
            foreach (var agentSection in agentsSection.GetChildren())
            {
                var agentDef = new AgentDefinition();
                agentSection.Bind(agentDef);
                _agents[agentSection.Key] = agentDef;
            }
        }
    }

    public string GetInstructions(string agentName, string defaultInstructions)
    {
        if (_agents.TryGetValue(agentName, out var agentDef) && !string.IsNullOrEmpty(agentDef.Instructions))
        {
            return agentDef.Instructions;
        }
        return defaultInstructions;
    }

    public string GetDescription(string agentName, string defaultDescription)
    {
        if (_agents.TryGetValue(agentName, out var agentDef) && !string.IsNullOrEmpty(agentDef.Metadata.Description))
        {
            return agentDef.Metadata.Description;
        }
        return defaultDescription;
    }
}