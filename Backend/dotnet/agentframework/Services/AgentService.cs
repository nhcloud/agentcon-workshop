using Microsoft.Extensions.Options;
using DotNetAgentFramework.Configuration;
using DotNetAgentFramework.Agents;

namespace DotNetAgentFramework.Services;

public interface IAgentService
{
    Task<IEnumerable<AgentInfo>> GetAvailableAgentsAsync();
    Task<IAgent?> GetAgentAsync(string agentName);
    Task<ChatResponse> ChatWithAgentAsync(string agentName, ChatRequest request);
    Task<ChatResponse> ChatWithAgentAsync(string agentName, ChatRequest request, List<GroupChatMessage>? conversationHistory);
    Task<IAgent?> CreateAzureFoundryAgentAsync(string agentType);
}

public class AgentService : IAgentService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentService> _logger;
    private readonly AzureAIConfig _azureConfig;
    private readonly IConfiguration _configuration;
    private readonly IOptions<AzureAIConfig> _azureConfigOptions;
    private readonly Dictionary<string, Func<Task<IAgent>>> _agentFactories;
    
    // Add caching for Azure AI Foundry agents to prevent multiple initializations
    private readonly Dictionary<string, IAgent> _foundryAgentCache = new();
    private readonly SemaphoreSlim _foundryAgentCacheLock = new(1, 1);

    public AgentService(
        IServiceProvider serviceProvider, 
        ILogger<AgentService> logger,
        IOptions<AzureAIConfig> azureConfig,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _azureConfig = azureConfig.Value;
        _azureConfigOptions = azureConfig;
        _configuration = configuration;
        
        // Initialize agent factories with async initialization
        _agentFactories = new Dictionary<string, Func<Task<IAgent>>>
        {
            ["people_lookup"] = async () => await CreateStandardAgentAsync<PeopleLookupAgent>(),
            ["knowledge_finder"] = async () => await CreateStandardAgentAsync<KnowledgeFinderAgent>(),
            ["generic_agent"] = async () => await CreateStandardAgentAsync<GenericAgent>(),
            ["generic"] = async () => await CreateStandardAgentAsync<GenericAgent>() // Backward compatibility
        };
    }

    private async Task<IAgent> CreateStandardAgentAsync<T>() where T : BaseAgent
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<T>>();
        var instructionsService = _serviceProvider.GetRequiredService<AgentInstructionsService>();
        
        // Create agent with configuration support
        var agent = (T)Activator.CreateInstance(typeof(T), logger, instructionsService, _azureConfigOptions)!;
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

        // Check cache first to prevent multiple initializations
        var cacheKey = $"foundry_{agentType}";
        if (_foundryAgentCache.TryGetValue(cacheKey, out var cachedAgent))
        {
            _logger.LogDebug("Returning cached Azure AI Foundry agent: {AgentType}", agentType);
            return cachedAgent;
        }

        await _foundryAgentCacheLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_foundryAgentCache.TryGetValue(cacheKey, out cachedAgent))
            {
                _logger.LogDebug("Returning cached Azure AI Foundry agent after lock: {AgentType}", agentType);
                return cachedAgent;
            }

            _logger.LogInformation("Creating new Azure AI Foundry agent: {AgentType}", agentType);
            
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

            // Get Azure OpenAI configuration for the foundry agent
            var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? _azureConfig?.AzureOpenAI?.DeploymentName ?? "";
            
            if (string.IsNullOrEmpty(deploymentName))
            {
                throw new InvalidOperationException("Azure OpenAI deployment name is required for Azure AI Foundry agent");
            }

            Azure.Core.TokenCredential? credential = null;
            
            if (!string.IsNullOrEmpty(_azureConfig.AzureAIFoundry.ManagedIdentityClientId))
            {
                credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = _azureConfig.AzureAIFoundry.ManagedIdentityClientId
                });
            }
            else
            {
                credential = new DefaultAzureCredential();
            }

            var foundryAgent = new AzureAIFoundryAgent(
                name: $"foundry_{agentType}",
                agentId: agentId,
                projectEndpoint: _azureConfig.AzureAIFoundry.ProjectEndpoint,
                description: description,
                instructions: instructions,
                modelDeployment: deploymentName,
                credential: credential,
                logger: logger,
                managedIdentityClientId: _azureConfig.AzureAIFoundry.ManagedIdentityClientId
            );

            await foundryAgent.InitializeAsync();
            
            // Cache the agent to prevent future reinitializations
            _foundryAgentCache[cacheKey] = foundryAgent;
            
            _logger.LogInformation("Created and cached Azure AI Foundry agent: {AgentName} with ID: {AgentId}", foundryAgent.Name, agentId);
            return foundryAgent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Azure AI Foundry agent for type {AgentType}", agentType);
            return null;
        }
        finally
        {
            _foundryAgentCacheLock.Release();
        }
    }

    public async Task<IEnumerable<AgentInfo>> GetAvailableAgentsAsync()
    {
        var agents = new List<AgentInfo>();

        // Check Azure AI Foundry configuration
        var hasFoundryConfig = _azureConfig?.AzureAIFoundry != null && 
                              !string.IsNullOrEmpty(_azureConfig.AzureAIFoundry.ProjectEndpoint);

        _logger.LogInformation("Azure AI Foundry configured: {HasFoundry}", hasFoundryConfig);

        // Always add the generic agent (Azure OpenAI)
        try
        {
            var genericAgent = await _agentFactories["generic_agent"]();
            agents.Add(new AgentInfo
            {
                Name = genericAgent.Name,
                Id = genericAgent.Name, // Set Id to be the same as Name
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
                            Id = foundryAgent.Name, // Set Id to be the same as Name
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
                    Id = standardAgent.Name, // Set Id to be the same as Name
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
                    "generic_agent" => true, // Generic agent also has a Foundry version
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
        return await ChatWithAgentAsync(agentName, request, null);
    }

    public async Task<ChatResponse> ChatWithAgentAsync(string agentName, ChatRequest request, List<GroupChatMessage>? conversationHistory)
    {
        var agent = await GetAgentAsync(agentName);
        if (agent == null)
        {
            throw new ArgumentException($"Agent '{agentName}' not found");
        }

        try
        {
            _logger.LogInformation("Starting chat with agent {AgentName} for message: {Message}", agentName, request.Message);
            var response = await agent.ChatWithHistoryAsync(request, conversationHistory);
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