using DotNetAgentFramework.Configuration;

namespace DotNetAgentFramework.Services;

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

    public string GetInstructions(string agentName)
    {
        if (_agents.TryGetValue(agentName, out var agentDef) && !string.IsNullOrEmpty(agentDef.Instructions))
        {
            return agentDef.Instructions;
        }
        
        throw new InvalidOperationException($"Instructions not found for agent '{agentName}' in configuration. Please ensure the agent is properly configured in config.yml.");
    }

    public string GetDescription(string agentName)
    {
        if (_agents.TryGetValue(agentName, out var agentDef) && !string.IsNullOrEmpty(agentDef.Metadata.Description))
        {
            return agentDef.Metadata.Description;
        }
        
        throw new InvalidOperationException($"Description not found for agent '{agentName}' in configuration. Please ensure the agent is properly configured in config.yml.");
    }
}