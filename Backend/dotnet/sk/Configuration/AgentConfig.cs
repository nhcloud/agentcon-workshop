namespace DotNetSemanticKernel.Configuration;

public class AgentConfig
{
    public Dictionary<string, AgentDefinition> Agents { get; set; } = new();
}

public class AgentDefinition
{
    public string Type { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string Instructions { get; set; } = string.Empty;
    public AgentMetadata Metadata { get; set; } = new();
    public FrameworkConfig FrameworkConfig { get; set; } = new();
}

public class AgentMetadata
{
    public string Description { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = new();
}

public class FrameworkConfig
{
    public string Provider { get; set; } = string.Empty;
    public string? AgentId { get; set; }
    public string? ProjectEndpoint { get; set; }
}

public class AppConfig
{
    public AgentConfig Agents { get; set; } = new();
    public AzureOpenAIConfig AzureOpenAI { get; set; } = new();
    public AzureAIFoundryConfig AzureFoundry { get; set; } = new();
    // Add other config sections as needed
}