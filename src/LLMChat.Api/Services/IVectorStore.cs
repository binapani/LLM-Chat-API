using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public interface IVectorStore
{
    Task AddAsync(DocumentVector document);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(float[] queryEmbedding, int topK);
}