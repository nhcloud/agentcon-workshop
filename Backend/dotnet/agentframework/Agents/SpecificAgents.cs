using DotNetAgentFramework.Services;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using DotNetAgentFramework.Configuration;

namespace DotNetAgentFramework.Agents;

/// <summary>
/// People Lookup Agent - Azure AI Foundry agent for finding people information
/// </summary>
public class PeopleLookupAgent(ILogger logger, AgentInstructionsService instructionsService, IOptions<AzureAIConfig>? azureConfig = null) : BaseAgent(logger)
{
    private readonly AgentInstructionsService _instructionsService = instructionsService;
    private readonly AzureAIConfig? _azureConfig = azureConfig?.Value;

    public override string Name => "people_lookup";
    public override string Description => _instructionsService.GetDescription("people_lookup");
    
    public override string Instructions => _instructionsService.GetInstructions("people_lookup");

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        try
        {
            // Get Azure OpenAI configuration
            var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? _azureConfig?.AzureOpenAI?.Endpoint;
            var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? _azureConfig?.AzureOpenAI?.ApiKey;
            var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? _azureConfig?.AzureOpenAI?.DeploymentName ?? "gpt-4o";

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Azure OpenAI endpoint and API key are required for People Lookup Agent");
            }

            // Create Azure OpenAI client and get chat client
            var azureClient = new AzureOpenAIClient(new Uri(endpoint), new Azure.AzureKeyCredential(apiKey));
            var chatClient = azureClient.GetChatClient(deploymentName);
            
            // Set the chat client for the base agent to use
            SetChatClient(chatClient);
            
            _logger.LogInformation("Initialized People Lookup Agent with Azure OpenAI deployment: {DeploymentName}", deploymentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize People Lookup Agent");
            throw;
        }
    }
}

/// <summary>
/// Knowledge Finder Agent - Azure AI Foundry agent for searching and retrieving information
/// </summary>
public class KnowledgeFinderAgent(ILogger logger, AgentInstructionsService instructionsService, IOptions<AzureAIConfig>? azureConfig = null) : BaseAgent(logger)
{
    private readonly AgentInstructionsService _instructionsService = instructionsService;
    private readonly AzureAIConfig? _azureConfig = azureConfig?.Value;

    public override string Name => "knowledge_finder";
    public override string Description => _instructionsService.GetDescription("knowledge_finder");
    
    public override string Instructions => _instructionsService.GetInstructions("knowledge_finder");

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        try
        {
            // Get Azure OpenAI configuration
            var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? _azureConfig?.AzureOpenAI?.Endpoint;
            var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? _azureConfig?.AzureOpenAI?.ApiKey;
            var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? _azureConfig?.AzureOpenAI?.DeploymentName ?? "gpt-4o";

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Azure OpenAI endpoint and API key are required for Knowledge Finder Agent");
            }

            // Create Azure OpenAI client and get chat client
            var azureClient = new AzureOpenAIClient(new Uri(endpoint), new Azure.AzureKeyCredential(apiKey));
            var chatClient = azureClient.GetChatClient(deploymentName);
            
            // Set the chat client for the base agent to use
            SetChatClient(chatClient);
            
            _logger.LogInformation("Initialized Knowledge Finder Agent with Azure OpenAI deployment: {DeploymentName}", deploymentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Knowledge Finder Agent");
            throw;
        }
    }
}

/// <summary>
/// Generic Agent - Default agent for general-purpose conversations (Azure OpenAI)
/// </summary>
public class GenericAgent(ILogger logger, AgentInstructionsService instructionsService, IOptions<AzureAIConfig>? azureConfig = null) : BaseAgent(logger)
{
    private readonly AgentInstructionsService _instructionsService = instructionsService;
    private readonly AzureAIConfig? _azureConfig = azureConfig?.Value;

    public override string Name => "generic_agent";
    public override string Description => _instructionsService.GetDescription("generic_agent");
    
    public override string Instructions => _instructionsService.GetInstructions("generic_agent");

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        try
        {
            // Get Azure OpenAI configuration
            var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? _azureConfig?.AzureOpenAI?.Endpoint;
            var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? _azureConfig?.AzureOpenAI?.ApiKey;
            var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? _azureConfig?.AzureOpenAI?.DeploymentName ?? "gpt-4o";

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Azure OpenAI endpoint and API key are required for Generic Agent");
            }

            // Create Azure OpenAI client and get chat client
            var azureClient = new AzureOpenAIClient(new Uri(endpoint), new Azure.AzureKeyCredential(apiKey));
            var chatClient = azureClient.GetChatClient(deploymentName);
            
            // Set the chat client for the base agent to use
            SetChatClient(chatClient);
            
            _logger.LogInformation("Initialized Generic Agent with Azure OpenAI deployment: {DeploymentName}", deploymentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Generic Agent");
            throw;
        }
    }
}