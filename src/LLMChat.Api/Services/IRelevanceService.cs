namespace LLMChat.Api.Services;

public interface IRelevanceService
{
    Task<bool> IsRelevantAsync(string question, string context);
}
