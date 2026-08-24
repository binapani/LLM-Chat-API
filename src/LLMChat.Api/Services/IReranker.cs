using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public interface IReranker
{
    Task<IReadOnlyList<RerankedResult>> RerankAsync(
        string query,
        IReadOnlyList<VectorSearchResult> candidates,
        int topK);
}
