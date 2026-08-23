namespace LLMChat.Api.Services;

public interface IRAGService
{
    Task<string> GenerateAnswerAsync(string question);
}