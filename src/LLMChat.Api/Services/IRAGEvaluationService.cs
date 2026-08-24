using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public interface IRAGEvaluationService
{
    Task<IReadOnlyList<RAGEvaluationResult>> EvaluateAsync(
        IReadOnlyList<RAGEvaluationCase> cases);
}
