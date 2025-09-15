using DotNetSemanticKernel.Models;
using DotNetSemanticKernel.Agents;
using Microsoft.SemanticKernel;

namespace DotNetSemanticKernel.Services;

public interface IAgentService
{
    Task<IEnumerable<AgentInfo>> GetAvailableAgentsAsync();
    Task<IAgent?> GetAgentAsync(string agentName);
    Task<ChatResponse> ChatWithAgentAsync(string agentName, ChatRequest request);
}

public class AgentService : IAgentService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Kernel _kernel;
    private readonly ILogger<AgentService> _logger;
    private readonly Dictionary<string, Func<IAgent>> _agentFactories;

    public AgentService(IServiceProvider serviceProvider, Kernel kernel, ILogger<AgentService> logger)
    {
        _serviceProvider = serviceProvider;
        _kernel = kernel;
        _logger = logger;
        
        _agentFactories = new Dictionary<string, Func<IAgent>>
        {
            ["people_lookup"] = () => new PeopleLookupAgent(_kernel, _serviceProvider.GetRequiredService<ILogger<PeopleLookupAgent>>()),
            ["knowledge_finder"] = () => new KnowledgeFinderAgent(_kernel, _serviceProvider.GetRequiredService<ILogger<KnowledgeFinderAgent>>()),
            ["task_assistant"] = () => new TaskAssistantAgent(_kernel, _serviceProvider.GetRequiredService<ILogger<TaskAssistantAgent>>()),
            ["technical_advisor"] = () => new TechnicalAdvisorAgent(_kernel, _serviceProvider.GetRequiredService<ILogger<TechnicalAdvisorAgent>>())
        };
    }

    public Task<IEnumerable<AgentInfo>> GetAvailableAgentsAsync()
    {
        var agents = _agentFactories.Values.Select(factory =>
        {
            var agent = factory();
            return new AgentInfo
            {
                Name = agent.Name,
                Description = agent.Description,
                Instructions = agent.Instructions,
                Model = "gpt-4o" // Default model
            };
        });

        return Task.FromResult(agents);
    }

    public Task<IAgent?> GetAgentAsync(string agentName)
    {
        if (_agentFactories.TryGetValue(agentName.ToLowerInvariant(), out var factory))
        {
            return Task.FromResult<IAgent?>(factory());
        }
        return Task.FromResult<IAgent?>(null);
    }

    public async Task<ChatResponse> ChatWithAgentAsync(string agentName, ChatRequest request)
    {
        var agent = await GetAgentAsync(agentName);
        if (agent == null)
        {
            throw new ArgumentException($"Agent '{agentName}' not found");
        }

        return await agent.ChatAsync(request);
    }
}