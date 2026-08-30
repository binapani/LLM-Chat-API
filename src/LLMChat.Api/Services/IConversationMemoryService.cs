using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public interface IConversationMemoryService
{
    Task<IReadOnlyList<OllamaMessage>> GetMessagesAsync(string sessionId);

    Task AddMessagesAsync(
        string sessionId,
        IEnumerable<OllamaMessage> messages);

    Task ReplaceMessagesAsync(
        string sessionId,
        IEnumerable<OllamaMessage> messages);

    Task ClearAsync(string sessionId);
}
