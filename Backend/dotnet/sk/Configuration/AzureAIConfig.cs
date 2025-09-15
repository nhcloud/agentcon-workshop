namespace DotNetSemanticKernel.Configuration;

public class AzureAIConfig
{
    public AzureOpenAIConfig? AzureOpenAI { get; set; }
    public AzureAIInferenceConfig? AzureAIInference { get; set; }
}

public class AzureOpenAIConfig
{
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? ChatDeployment { get; set; }
    public string? EmbeddingDeployment { get; set; }
    public string? ApiVersion { get; set; }
}

public class AzureAIInferenceConfig
{
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? ModelName { get; set; }
}