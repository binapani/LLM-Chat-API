namespace LLMChat.Api.Services;

public interface IAgentService
{
    Task<string> RunAsync(
        string userMessage,
        CancellationToken cancellationToken);
}