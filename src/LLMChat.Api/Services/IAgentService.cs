namespace LLMChat.Api.Services;

public interface IAgentService
{
    Task<string> RunAsync(
        string sessionId,
        string userMessage,
        CancellationToken cancellationToken);
}