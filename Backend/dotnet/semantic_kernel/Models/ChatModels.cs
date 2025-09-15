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
}

public class ChatResponse
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("agent")]
    public string Agent { get; set; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("usage")]
    public UsageInfo? Usage { get; set; }
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
}

public class GroupChatRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("agents")]
    public List<string> Agents { get; set; } = new();

    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    [JsonPropertyName("max_turns")]
    public int MaxTurns { get; set; } = 5;
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
}