using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public interface IVectorStore
{
    Task AddAsync(DocumentVector document);
    Task<IReadOnlyList<DocumentVector>> SearchAsync(float[] queryEmbedding, int topK);
}