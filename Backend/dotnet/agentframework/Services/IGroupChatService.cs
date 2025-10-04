using DotNetAgentFramework.Models;

namespace DotNetAgentFramework.Services;

/// <summary>
/// Service interface for handling group chat functionality with multiple agents using Microsoft Agent Framework
/// </summary>
public interface IGroupChatService
{
    /// <summary>
    /// Start a group chat session with multiple agents using Microsoft Agent Framework
    /// </summary>
    /// <param name="request">Group chat request containing message, agents, and configuration</param>
    /// <returns>Group chat response with agent messages and conversation summary</returns>
    Task<GroupChatResponse> StartGroupChatAsync(GroupChatRequest request);

    /// <summary>
    /// Summarize a conversation between multiple agents
    /// </summary>
    /// <param name="messages">List of messages to summarize</param>
    /// <returns>Conversation summary</returns>
    Task<string> SummarizeConversationAsync(List<GroupChatMessage> messages);

    /// <summary>
    /// Summarize a conversation with cancellation token support
    /// </summary>
    /// <param name="messages">List of messages to summarize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Conversation summary</returns>
    Task<string> SummarizeConversationAsync(List<GroupChatMessage> messages, CancellationToken cancellationToken);
}