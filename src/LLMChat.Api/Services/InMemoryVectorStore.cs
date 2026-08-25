using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public class InMemoryVectorStore : IVectorStore
{
    private readonly List<DocumentVector> _documents = new();

    public Task AddAsync(DocumentVector document)
    {
        _documents.Add(document);
        return Task.CompletedTask;
    }

    public Task ReplaceAsync(
        string documentId,
        IReadOnlyList<DocumentVector> documents,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _documents.RemoveAll(document => document.DocumentId == documentId);
        _documents.AddRange(documents);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(float[] queryEmbedding, int topK)
    {
        var results = _documents
            .Select(document => new VectorSearchResult
            {
                Document = document,
                Similarity = CalculateCosineSimilarity(queryEmbedding, document.Embedding)
            })
            .OrderByDescending(result => result.Similarity)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(results);
    }

    private static float CalculateCosineSimilarity(float[] first, float[] second)
    {
        if (first.Length == 0 || second.Length == 0 || first.Length != second.Length)
        {
            return 0;
        }

        float dotProduct = 0;
        float firstMagnitude = 0;
        float secondMagnitude = 0;

        for (var index = 0; index < first.Length; index++)
        {
            dotProduct += first[index] * second[index];
            firstMagnitude += first[index] * first[index];
            secondMagnitude += second[index] * second[index];
        }

        if (firstMagnitude == 0 || secondMagnitude == 0)
        {
            return 0;
        }

        return dotProduct / (MathF.Sqrt(firstMagnitude) * MathF.Sqrt(secondMagnitude));
    }
}