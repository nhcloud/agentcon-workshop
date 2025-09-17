using System.Collections.Concurrent;
using DotNetSemanticKernel.Models;

namespace DotNetSemanticKernel.Services;

public interface ISessionManager
{
    Task<string> CreateSessionAsync();
    Task<List<GroupChatMessage>> GetSessionHistoryAsync(string sessionId);
    Task AddMessageToSessionAsync(string sessionId, GroupChatMessage message);
    Task ClearSessionAsync(string sessionId);
    Task<bool> SessionExistsAsync(string sessionId);
    Task<IEnumerable<string>> GetActiveSessionsAsync();
    Task<SessionInfo> GetSessionInfoAsync(string sessionId);
}

public class SessionManager : ISessionManager
{
    private readonly ConcurrentDictionary<string, List<GroupChatMessage>> _sessions = new();
    private readonly ConcurrentDictionary<string, DateTime> _sessionCreationTimes = new();
    private readonly ILogger<SessionManager> _logger;

    public SessionManager(ILogger<SessionManager> logger)
    {
        _logger = logger;
    }

    public Task<string> CreateSessionAsync()
    {
        var sessionId = Guid.NewGuid().ToString();
        _sessions[sessionId] = new List<GroupChatMessage>();
        _sessionCreationTimes[sessionId] = DateTime.UtcNow;
        _logger.LogInformation("Created new session: {SessionId}", sessionId);
        return Task.FromResult(sessionId);
    }

    public Task<List<GroupChatMessage>> GetSessionHistoryAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var history))
        {
            return Task.FromResult(new List<GroupChatMessage>(history));
        }
        return Task.FromResult(new List<GroupChatMessage>());
    }

    public Task AddMessageToSessionAsync(string sessionId, GroupChatMessage message)
    {
        if (!_sessions.ContainsKey(sessionId))
        {
            _sessions[sessionId] = new List<GroupChatMessage>();
            _sessionCreationTimes[sessionId] = DateTime.UtcNow;
        }

        _sessions[sessionId].Add(message);
        _logger.LogDebug("Added message to session {SessionId} from agent {Agent}", sessionId, message.Agent);
        return Task.CompletedTask;
    }

    public Task ClearSessionAsync(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
        _sessionCreationTimes.TryRemove(sessionId, out _);
        _logger.LogInformation("Cleared session: {SessionId}", sessionId);
        return Task.CompletedTask;
    }

    public Task<bool> SessionExistsAsync(string sessionId)
    {
        return Task.FromResult(_sessions.ContainsKey(sessionId));
    }

    public Task<IEnumerable<string>> GetActiveSessionsAsync()
    {
        var activeSessions = _sessions.Keys.ToList();
        _logger.LogDebug("Retrieved {SessionCount} active sessions", activeSessions.Count);
        return Task.FromResult<IEnumerable<string>>(activeSessions);
    }

    public Task<SessionInfo> GetSessionInfoAsync(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var history))
        {
            throw new ArgumentException($"Session {sessionId} not found");
        }

        var creationTime = _sessionCreationTimes.TryGetValue(sessionId, out var created) ? created : DateTime.UtcNow;
        var lastActivity = history.LastOrDefault()?.Timestamp ?? creationTime;
        var agentTypes = history.Where(m => m.Agent != "user")
                              .Select(m => m.Agent)
                              .Distinct()
                              .ToList();

        var sessionInfo = new SessionInfo
        {
            SessionId = sessionId,
            CreatedAt = creationTime,
            LastActivity = lastActivity,
            MessageCount = history.Count,
            AgentTypes = agentTypes
        };

        return Task.FromResult(sessionInfo);
    }
}