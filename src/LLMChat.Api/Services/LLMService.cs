namespace LLMChat.Api.Services;

public class LLMService : ILLMService
{
    public Task<string> GenerateAnswerAsync(string userMessage)
    {
        return Task.FromResult($"LLM service is working. You said: {userMessage}");
    }
}
