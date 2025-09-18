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
            ["people_lookup"] = async () => await CreateStandardAgentAsync<PeopleLookupAgent>(),
            ["knowledge_finder"] = async () => await CreateStandardAgentAsync<KnowledgeFinderAgent>(),
            ["generic"] = async () => await CreateStandardAgentAsync<GenericAgent>()
        };
    }

    private async Task<IAgent> CreateStandardAgentAsync<T>() where T : BaseAgent
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
                    "Azure AI Foundry People Lookup Agent with enterprise directory access",
                    "You are a specialized People Lookup agent running in Azure AI Foundry. You have access to enterprise people directory and contact information. Use your enterprise knowledge to help users find the right people for their needs."
                ),
                "knowledge_finder" => (
                    _azureConfig.AzureAIFoundry.KnowledgeAgentId ?? "knowledge-agent",
                    "Azure AI Foundry Knowledge Finder Agent with enterprise knowledge access", 
                    "You are a specialized Knowledge Finder agent running in Azure AI Foundry. You have access to enterprise knowledge bases, document repositories, and specialized information systems. Help users find the most relevant and accurate information from enterprise sources."
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
            _logger.LogInformation("Created Azure AI Foundry agent: {AgentName} with ID: {AgentId}", foundryAgent.Name, agentId);
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

        // Check Azure AI Foundry configuration
        var hasFoundryConfig = _azureConfig?.AzureAIFoundry != null && 
                              !string.IsNullOrEmpty(_azureConfig.AzureAIFoundry.ProjectEndpoint);

        _logger.LogInformation("Azure AI Foundry configured: {HasFoundry}", hasFoundryConfig);

        // Generic agent (always available as Azure OpenAI)
        try
        {
            var genericAgent = await _agentFactories["generic"]();
            agents.Add(new AgentInfo
            {
                Name = genericAgent.Name,
                Description = genericAgent.Description,
                Instructions = genericAgent.Instructions,
                Model = "Azure OpenAI GPT-4o",
                AgentType = "Azure OpenAI",
                Capabilities = new List<string> { "General conversation", "Problem solving", "Task assistance", "Information provision" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create generic agent info");
        }

        // People Lookup agent
        await AddAgentInfo(agents, "people_lookup", hasFoundryConfig, 
            new List<string> { "People search", "Contact discovery", "Team coordination", "Role identification" });

        // Knowledge Finder agent  
        await AddAgentInfo(agents, "knowledge_finder", hasFoundryConfig,
            new List<string> { "Document search", "Knowledge retrieval", "Research assistance", "Information synthesis" });

        _logger.LogInformation("Returning {AgentCount} available agents", agents.Count);
        return agents;
    }

    private async Task AddAgentInfo(List<AgentInfo> agents, string agentType, bool hasFoundryConfig, List<string> capabilities)
    {
        // Try Azure AI Foundry first if configured
        if (hasFoundryConfig)
        {
            var foundryConfig = _azureConfig?.AzureAIFoundry;
            var hasAgentId = agentType switch
            {
                "people_lookup" => !string.IsNullOrEmpty(foundryConfig?.PeopleAgentId),
                "knowledge_finder" => !string.IsNullOrEmpty(foundryConfig?.KnowledgeAgentId),
                _ => false
            };

            if (hasAgentId)
            {
                try
                {
                    var foundryAgent = await CreateAzureFoundryAgentAsync(agentType);
                    if (foundryAgent != null)
                    {
                        agents.Add(new AgentInfo
                        {
                            Name = foundryAgent.Name,
                            Description = foundryAgent.Description,
                            Instructions = foundryAgent.Instructions,
                            Model = "Azure AI Foundry",
                            AgentType = "Azure AI Foundry",
                            Capabilities = capabilities
                        });
                        
                        _logger.LogInformation("Added Azure AI Foundry agent: {AgentName}", foundryAgent.Name);
                        return; // Successfully added Foundry agent, don't add standard version
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create Azure AI Foundry agent {AgentType}, falling back to standard", agentType);
                }
            }
        }

        // Add standard Azure OpenAI agent as fallback
        try
        {
            if (_agentFactories.TryGetValue(agentType, out var factory))
            {
                var standardAgent = await factory();
                agents.Add(new AgentInfo
                {
                    Name = standardAgent.Name,
                    Description = standardAgent.Description,
                    Instructions = standardAgent.Instructions,
                    Model = "Azure OpenAI GPT-4o",
                    AgentType = "Azure OpenAI",
                    Capabilities = capabilities
                });
                
                _logger.LogInformation("Added Azure OpenAI agent: {AgentName}", standardAgent.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create standard agent {AgentType}", agentType);
        }
    }

    public async Task<IAgent?> GetAgentAsync(string agentName)
    {
        var normalizedName = agentName.ToLowerInvariant();
        
        _logger.LogInformation("Retrieving agent: {AgentName}", agentName);

        // Check for Azure AI Foundry agents first
        if (normalizedName.StartsWith("foundry_"))
        {
            var baseType = normalizedName.Substring("foundry_".Length);
            var foundryAgent = await CreateAzureFoundryAgentAsync(baseType);
            if (foundryAgent != null)
            {
                _logger.LogInformation("Retrieved Azure AI Foundry agent: {AgentName}", foundryAgent.Name);
                return foundryAgent;
            }
        }

        // For agents without foundry_ prefix, determine the best agent to use
        if (_agentFactories.TryGetValue(normalizedName, out var factory))
        {
            // Check if we should use Azure AI Foundry version
            var hasFoundryConfig = _azureConfig?.AzureAIFoundry != null && 
                                  !string.IsNullOrEmpty(_azureConfig.AzureAIFoundry.ProjectEndpoint);

            if (hasFoundryConfig)
            {
                var hasAgentId = normalizedName switch
                {
                    "people_lookup" => !string.IsNullOrEmpty(_azureConfig.AzureAIFoundry.PeopleAgentId),
                    "knowledge_finder" => !string.IsNullOrEmpty(_azureConfig.AzureAIFoundry.KnowledgeAgentId),
                    _ => false
                };

                if (hasAgentId)
                {
                    try
                    {
                        var foundryAgent = await CreateAzureFoundryAgentAsync(normalizedName);
                        if (foundryAgent != null)
                        {
                            _logger.LogInformation("Using Azure AI Foundry agent for {AgentName}", agentName);
                            return foundryAgent;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create Azure AI Foundry agent {AgentName}, using standard version", agentName);
                    }
                }
            }

            // Use standard Azure OpenAI agent
            var standardAgent = await factory();
            _logger.LogInformation("Using Azure OpenAI agent for {AgentName}", agentName);
            return standardAgent;
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
            _logger.LogInformation("Starting chat with agent {AgentName} for message: {Message}", agentName, request.Message);
            var response = await agent.ChatAsync(request);
            _logger.LogInformation("Chat completed with agent {AgentName}, response length: {Length}", agentName, response.Content?.Length ?? 0);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during chat with agent {AgentName}", agentName);
            throw;
        }
    }
}