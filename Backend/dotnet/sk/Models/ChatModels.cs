using System.Text.Json.Serialization;

namespace DotNetSemanticKernel.Models;

public class ChatRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("agent")]
    public string? Agent { get; set; }

    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    [JsonPropertyName("agents")]
    public List<string>? Agents { get; set; }

    [JsonPropertyName("context")]
    public string? Context { get; set; }
}

public class ChatResponse
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("agent")]
    public string Agent { get; set; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("usage")]
    public UsageInfo? Usage { get; set; }

    [JsonPropertyName("processing_time_ms")]
    public int ProcessingTimeMs { get; set; }
}

public class UsageInfo
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public class AgentInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("instructions")]
    public string Instructions { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("agent_type")]
    public string AgentType { get; set; } = "Standard";

    [JsonPropertyName("capabilities")]
    public List<string> Capabilities { get; set; } = new();
}

public class GroupChatRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("agents")]
    public List<string>? Agents { get; set; } = null;

    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    [JsonPropertyName("max_turns")]
    public int MaxTurns { get; set; } = 2;

    [JsonPropertyName("use_semantic_kernel_groupchat")]
    public bool UseSemanticKernelGroupChat { get; set; } = false;

    [JsonPropertyName("context")]
    public string? Context { get; set; }

    [JsonPropertyName("config")]
    public object? Config { get; set; }

    [JsonPropertyName("summarize")]
    public bool Summarize { get; set; } = true;

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "sequential";
}

public class GroupChatResponse
{
    [JsonPropertyName("messages")]
    public List<GroupChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("total_turns")]
    public int TotalTurns { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("group_chat_type")]
    public string? GroupChatType { get; set; }

    [JsonPropertyName("agent_count")]
    public int AgentCount { get; set; }

    [JsonPropertyName("total_processing_time_ms")]
    public int TotalProcessingTimeMs { get; set; }
}

public class GroupChatMessage
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("agent")]
    public string Agent { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("turn")]
    public int Turn { get; set; }

    [JsonPropertyName("agent_type")]
    public string? AgentType { get; set; }

    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Azure AI Foundry specific models
/// </summary>
public class AzureFoundryAgentRequest
{
    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("thread_id")]
    public string? ThreadId { get; set; }

    [JsonPropertyName("additional_instructions")]
    public string? AdditionalInstructions { get; set; }
}

public class AzureFoundryAgentResponse
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = string.Empty;

    [JsonPropertyName("thread_id")]
    public string ThreadId { get; set; } = string.Empty;

    [JsonPropertyName("run_id")]
    public string? RunId { get; set; }

    [JsonPropertyName("usage")]
    public UsageInfo? Usage { get; set; }
}

/// <summary>
/// Session and history management models
/// </summary>
public class SessionInfo
{
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("last_activity")]
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("message_count")]
    public int MessageCount { get; set; }

    [JsonPropertyName("agent_types")]
    public List<string> AgentTypes { get; set; } = new();
}

/// <summary>
/// Health and configuration check models
/// </summary>
public class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "healthy";

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("framework")]
    public string Framework { get; set; } = ".NET 9";

    [JsonPropertyName("configuration")]
    public ConfigurationStatus Configuration { get; set; } = new();
}

public class ConfigurationStatus
{
    [JsonPropertyName("azure_openai")]
    public string AzureOpenAI { get; set; } = "missing";

    [JsonPropertyName("azure_ai_foundry")]
    public string AzureAIFoundry { get; set; } = "missing";

    [JsonPropertyName("frontend_url")]
    public string FrontendUrl { get; set; } = string.Empty;

    [JsonPropertyName("configuration_source")]
    public string ConfigurationSource { get; set; } = "unknown";
}