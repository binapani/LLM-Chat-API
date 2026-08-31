using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public interface IBm25SearchService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task BackfillAsync(CancellationToken cancellationToken = default);

    Task IndexChunkAsync(DocumentVector document, CancellationToken cancellationToken = default);

    Task ReindexDocumentAsync(
        string documentId,
        IReadOnlyList<DocumentVector> documents,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Bm25SearchResult>> SearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken = default);
}
