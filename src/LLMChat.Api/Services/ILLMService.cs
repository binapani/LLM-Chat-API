namespace LLMChat.Api.Services;

public interface ILLMService
{
    Task<string> GenerateAnswerAsync(string userMessage);
}
