using System.Collections.Concurrent;
using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public class ConversationMemoryService : IConversationMemoryService
{
    private readonly ConcurrentDictionary<string, List<OllamaMessage>> _sessions =
        new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, object> _sessionLocks =
        new(StringComparer.Ordinal);

    private readonly ILogger<ConversationMemoryService> _logger;

    public ConversationMemoryService(ILogger<ConversationMemoryService> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<OllamaMessage>> GetMessagesAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID is required.", nameof(sessionId));
        }

        var sessionLock = _sessionLocks.GetOrAdd(sessionId, _ => new object());

        lock (sessionLock)
        {
            var messages = _sessions.TryGetValue(sessionId, out var sessionMessages)
                ? sessionMessages
                : new List<OllamaMessage>();

            var copy = messages.ToList();

            _logger.LogInformation(
                "Session {SessionId}: read {PreviousMessageCount} messages from memory.",
                sessionId,
                copy.Count);

            return Task.FromResult<IReadOnlyList<OllamaMessage>>(copy);
        }
    }

    public Task AddMessagesAsync(
        string sessionId,
        IEnumerable<OllamaMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID is required.", nameof(sessionId));
        }

        ArgumentNullException.ThrowIfNull(messages);

        var sessionLock = _sessionLocks.GetOrAdd(sessionId, _ => new object());

        lock (sessionLock)
        {
            var sessionMessages = _sessions.GetOrAdd(sessionId, _ => new List<OllamaMessage>());
            var messageList = messages.ToList();

            sessionMessages.AddRange(messageList);

            _logger.LogInformation(
                "Session {SessionId}: added {MessageCount} messages to memory.",
                sessionId,
                messageList.Count);
        }

        return Task.CompletedTask;
    }

    public Task ReplaceMessagesAsync(
        string sessionId,
        IEnumerable<OllamaMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID is required.", nameof(sessionId));
        }

        ArgumentNullException.ThrowIfNull(messages);

        var messageList = messages.ToList();
        var sessionLock = _sessionLocks.GetOrAdd(sessionId, _ => new object());

        lock (sessionLock)
        {
            var replacement = messageList.ToList();
            _sessions[sessionId] = replacement;

            _logger.LogInformation(
                "Session {SessionId}: replaced memory with {MessageCount} messages.",
                sessionId,
                replacement.Count);
        }

        return Task.CompletedTask;
    }

    public Task ClearAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID is required.", nameof(sessionId));
        }

        var sessionLock = _sessionLocks.GetOrAdd(sessionId, _ => new object());

        lock (sessionLock)
        {
            _sessions.TryRemove(sessionId, out _);
            _logger.LogInformation("Session {SessionId}: memory cleared.", sessionId);
        }

        return Task.CompletedTask;
    }
}
