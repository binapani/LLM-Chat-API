using System.Collections.Concurrent;
using System.Text.Json;
using LLMChat.Api.Data;
using LLMChat.Api.Data.Entities;
using LLMChat.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LLMChat.Api.Services;

public class EfConversationMemoryService : IConversationMemoryService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _sessionLocks =
        new(StringComparer.Ordinal);

    private readonly VectorDbContext _dbContext;
    private readonly ILogger<EfConversationMemoryService> _logger;

    public EfConversationMemoryService(
        VectorDbContext dbContext,
        ILogger<EfConversationMemoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OllamaMessage>> GetMessagesAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID is required.", nameof(sessionId));
        }

        var session = await _dbContext.ConversationSessions
            .AsNoTracking()
            .Include(s => s.Messages)
            .SingleOrDefaultAsync(s => s.SessionId == sessionId);

        if (session is null)
        {
            _logger.LogInformation("Session {SessionId}: no persisted conversation found.", sessionId);
            return Array.Empty<OllamaMessage>();
        }

        var messages = session.Messages
            .OrderBy(m => m.SequenceNumber)
            .Select(MapToOllamaMessage)
            .ToList();

        _logger.LogInformation(
            "Session {SessionId}: loaded {MessageCount} persisted messages.",
            sessionId,
            messages.Count);

        return messages;
    }

    public async Task AddMessagesAsync(
        string sessionId,
        IEnumerable<OllamaMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID is required.", nameof(sessionId));
        }

        ArgumentNullException.ThrowIfNull(messages);

        var messageList = messages.ToList();
        var sessionLock = _sessionLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));

        await sessionLock.WaitAsync();
        try
        {
            var session = await _dbContext.ConversationSessions
                .SingleOrDefaultAsync(s => s.SessionId == sessionId);

            if (session is null)
            {
                session = new ConversationSessionEntity
                {
                    SessionId = sessionId,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                _dbContext.ConversationSessions.Add(session);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Session {SessionId}: created persisted conversation.", sessionId);
            }

            var nextSequence = await _dbContext.ConversationMessages
                .Where(m => m.ConversationSessionId == session.Id)
                .MaxAsync(m => (int?)m.SequenceNumber) ?? 0;

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            foreach (var message in messageList)
            {
                nextSequence += 1;

                _dbContext.ConversationMessages.Add(new ConversationMessageEntity
                {
                    ConversationSessionId = session.Id,
                    SequenceNumber = nextSequence,
                    Role = message.Role,
                    Content = message.Content,
                    ToolCallsJson = SerializeToolCalls(message.ToolCalls),
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            session.UpdatedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        finally
        {
            sessionLock.Release();
        }

        _logger.LogInformation(
            "Session {SessionId}: added {MessageCount} messages to persisted memory.",
            sessionId,
            messageList.Count);
    }

    public async Task ReplaceMessagesAsync(
        string sessionId,
        IEnumerable<OllamaMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID is required.", nameof(sessionId));
        }

        ArgumentNullException.ThrowIfNull(messages);

        var messageList = messages.ToList();
        var sessionLock = _sessionLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));

        await sessionLock.WaitAsync();
        try
        {
            var session = await _dbContext.ConversationSessions
                .SingleOrDefaultAsync(s => s.SessionId == sessionId);

            if (session is null)
            {
                session = new ConversationSessionEntity
                {
                    SessionId = sessionId,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                _dbContext.ConversationSessions.Add(session);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Session {SessionId}: created persisted conversation.", sessionId);
            }

            var now = DateTime.UtcNow;

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var existingMessages = await _dbContext.ConversationMessages
                .Where(m => m.ConversationSessionId == session.Id)
                .ToListAsync();

            if (existingMessages.Count > 0)
            {
                _dbContext.ConversationMessages.RemoveRange(existingMessages);
            }

            for (var index = 0; index < messageList.Count; index++)
            {
                var sourceMessage = messageList[index];

                _dbContext.ConversationMessages.Add(new ConversationMessageEntity
                {
                    ConversationSessionId = session.Id,
                    SequenceNumber = index + 1,
                    Role = sourceMessage.Role,
                    Content = sourceMessage.Content,
                    ToolCallsJson = SerializeToolCalls(sourceMessage.ToolCalls),
                    CreatedAtUtc = now
                });
            }

            session.UpdatedAtUtc = now;
            if (session.CreatedAtUtc == default)
            {
                session.CreatedAtUtc = now;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        finally
        {
            sessionLock.Release();
        }

        _logger.LogInformation(
            "Session {SessionId}: replaced persisted memory with {MessageCount} messages.",
            sessionId,
            messageList.Count);
    }

    public async Task ClearAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID is required.", nameof(sessionId));
        }

        var session = await _dbContext.ConversationSessions
            .SingleOrDefaultAsync(s => s.SessionId == sessionId);

        if (session is null)
        {
            _logger.LogInformation("Session {SessionId}: clear requested but no persisted conversation exists.", sessionId);
            return;
        }

        _dbContext.ConversationSessions.Remove(session);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Session {SessionId}: persisted conversation cleared.", sessionId);
    }

    private static OllamaMessage MapToOllamaMessage(ConversationMessageEntity entity)
    {
        return new OllamaMessage
        {
            Role = entity.Role,
            Content = entity.Content,
            ToolCalls = DeserializeToolCalls(entity.ToolCallsJson)
        };
    }

    private static List<OllamaToolCall>? DeserializeToolCalls(string? toolCallsJson)
    {
        if (string.IsNullOrWhiteSpace(toolCallsJson))
        {
            return null;
        }

        try
        {
            var toolCalls = JsonSerializer.Deserialize<List<OllamaToolCall>>(toolCallsJson, SerializerOptions);
            return toolCalls ?? new List<OllamaToolCall>();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? SerializeToolCalls(List<OllamaToolCall>? toolCalls)
    {
        if (toolCalls == null || toolCalls.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(toolCalls, SerializerOptions);
    }
}
