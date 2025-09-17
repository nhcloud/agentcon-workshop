using DotNetSemanticKernel.Models;
using DotNetSemanticKernel.Agents;
using DotNetSemanticKernel.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Options;

namespace DotNetSemanticKernel.Services;

public interface IAgentService
{
    Task<IEnumerable<AgentInfo>> GetAvailableAgentsAsync();
    Task<IAgent?> GetAgentAsync(string agentName);
    Task<ChatResponse> ChatWithAgentAsync(string agentName, ChatRequest request);
    Task<IAgent?> CreateAzureFoundryAgentAsync(string agentType);
}

public class AgentService : IAgentService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Kernel _kernel;
    private readonly ILogger<AgentService> _logger;
    private readonly AzureAIConfig _azureConfig;
    private readonly Dictionary<string, Func<Task<IAgent>>> _agentFactories;

    public AgentService(
        IServiceProvider serviceProvider, 
        Kernel kernel, 
        ILogger<AgentService> logger,
        IOptions<AzureAIConfig> azureConfig)
    {
        _serviceProvider = serviceProvider;
        _kernel = kernel;
        _logger = logger;
        _azureConfig = azureConfig.Value;
        
        // Initialize agent factories with async initialization
        _agentFactories = new Dictionary<string, Func<Task<IAgent>>>
        {
            ["people_lookup"] = async () => await CreateBasicAgentAsync<PeopleLookupAgent>(),
            ["knowledge_finder"] = async () => await CreateBasicAgentAsync<KnowledgeFinderAgent>(),
            ["task_assistant"] = async () => await CreateBasicAgentAsync<TaskAssistantAgent>(),
            ["technical_advisor"] = async () => await CreateBasicAgentAsync<TechnicalAdvisorAgent>(),
            ["creative_assistant"] = async () => await CreateBasicAgentAsync<CreativeAssistantAgent>(),
            ["generic"] = async () => await CreateBasicAgentAsync<GenericAgent>()
        };
    }

    private async Task<IAgent> CreateBasicAgentAsync<T>() where T : BaseAgent
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<T>>();
        var agent = (T)Activator.CreateInstance(typeof(T), _kernel, logger)!;
        await agent.InitializeAsync();
        return agent;
    }

    public async Task<IAgent?> CreateAzureFoundryAgentAsync(string agentType)
    {
        if (_azureConfig?.AzureAIFoundry == null || 
            string.IsNullOrEmpty(_azureConfig.AzureAIFoundry.ProjectEndpoint))
        {
            _logger.LogWarning("Azure AI Foundry not configured, cannot create foundry agent");
            return null;
        }

        try
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<AzureAIFoundryAgent>>();
            
            var (agentId, description, instructions) = agentType.ToLowerInvariant() switch
            {
                "people_lookup" => (
                    _azureConfig.AzureAIFoundry.PeopleAgentId ?? "people-agent",
                    "Azure AI Foundry People Lookup Agent",
                    "You are a specialized People Lookup agent running in Azure AI Foundry. You have access to enterprise people directory and contact information."
                ),
                "knowledge_finder" => (
                    _azureConfig.AzureAIFoundry.KnowledgeAgentId ?? "knowledge-agent",
                    "Azure AI Foundry Knowledge Finder Agent", 
                    "You are a specialized Knowledge Finder agent running in Azure AI Foundry. You have access to enterprise knowledge bases and document repositories."
                ),
                _ => throw new ArgumentException($"Azure AI Foundry agent type '{agentType}' not supported")
            };

            if (string.IsNullOrEmpty(agentId))
            {
                throw new InvalidOperationException($"Agent ID not configured for {agentType} in Azure AI Foundry");
            }

            var foundryAgent = new AzureAIFoundryAgent(
                name: $"foundry_{agentType}",
                agentId: agentId,
                projectEndpoint: _azureConfig.AzureAIFoundry.ProjectEndpoint,
                description: description,
                instructions: instructions,
                kernel: _kernel,
                logger: logger
            );

            await foundryAgent.InitializeAsync();
            return foundryAgent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Azure AI Foundry agent for type {AgentType}", agentType);
            return null;
        }
    }

    public async Task<IEnumerable<AgentInfo>> GetAvailableAgentsAsync()
    {
        var agents = new List<AgentInfo>();

        // Add basic agents
        foreach (var (name, factory) in _agentFactories)
        {
            try
            {
                var agent = await factory();
                agents.Add(new AgentInfo
                {
                    Name = agent.Name,
                    Description = agent.Description,
                    Instructions = agent.Instructions,
                    Model = "Azure OpenAI - GPT-4o",
                    AgentType = "Standard"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create agent info for {AgentName}", name);
            }
        }

        // Add Azure AI Foundry agents if configured
        if (_azureConfig?.AzureAIFoundry != null && 
            !string.IsNullOrEmpty(_azureConfig.AzureAIFoundry.ProjectEndpoint))
        {
            if (!string.IsNullOrEmpty(_azureConfig.AzureAIFoundry.PeopleAgentId))
            {
                agents.Add(new AgentInfo
                {
                    Name = "foundry_people_lookup",
                    Description = "Azure AI Foundry People Lookup Agent with enterprise directory access",
                    Instructions = "Enterprise-grade people lookup with Azure AI Foundry",
                    Model = "Azure AI Foundry",
                    AgentType = "Azure AI Foundry"
                });
            }

            if (!string.IsNullOrEmpty(_azureConfig.AzureAIFoundry.KnowledgeAgentId))
            {
                agents.Add(new AgentInfo
                {
                    Name = "foundry_knowledge_finder",
                    Description = "Azure AI Foundry Knowledge Finder Agent with enterprise knowledge access",
                    Instructions = "Enterprise-grade knowledge search with Azure AI Foundry",
                    Model = "Azure AI Foundry", 
                    AgentType = "Azure AI Foundry"
                });
            }
        }

        return agents;
    }

    public async Task<IAgent?> GetAgentAsync(string agentName)
    {
        var normalizedName = agentName.ToLowerInvariant();

        // Check for Azure AI Foundry agents
        if (normalizedName.StartsWith("foundry_"))
        {
            var baseType = normalizedName.Substring("foundry_".Length);
            return await CreateAzureFoundryAgentAsync(baseType);
        }

        // Check for standard agents
        if (_agentFactories.TryGetValue(normalizedName, out var factory))
        {
            return await factory();
        }

        _logger.LogWarning("Agent '{AgentName}' not found", agentName);
        return null;
    }

    public async Task<ChatResponse> ChatWithAgentAsync(string agentName, ChatRequest request)
    {
        var agent = await GetAgentAsync(agentName);
        if (agent == null)
        {
            throw new ArgumentException($"Agent '{agentName}' not found");
        }

        try
        {
            return await agent.ChatAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during chat with agent {AgentName}", agentName);
            throw;
        }
    }
}