using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public interface IHybridSearchService
{
    Task<IReadOnlyList<HybridSearchResult>> SearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken = default);
}
