using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public interface IReranker
{
    Task<IReadOnlyList<VectorSearchResult>> RerankAsync(
        string query,
        IReadOnlyList<VectorSearchResult> candidates,
        int topK);
}
