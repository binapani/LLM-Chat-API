using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public interface IHybridReranker
{
    Task<IReadOnlyList<HybridSearchResult>> RerankAsync(
        string query,
        IReadOnlyList<HybridSearchResult> candidates,
        int topK);
}
