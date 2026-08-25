using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public interface IVectorStore
{
    Task AddAsync(DocumentVector document);

    Task ReplaceAsync(
        string documentId,
        IReadOnlyList<DocumentVector> documents,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(float[] queryEmbedding, int topK);
}