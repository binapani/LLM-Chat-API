using LLMChat.Api.Data;
using LLMChat.Api.Data.Entities;
using LLMChat.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LLMChat.Api.Services;

public class SQLiteVectorStore : IVectorStore
{
    private readonly VectorDbContext _dbContext;

    public SQLiteVectorStore(VectorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DocumentVector document)
    {
        var entity = new DocumentVectorEntity
        {
            DocumentId = document.DocumentId,
            ChunkId = document.ChunkId,
            Source = document.Source,
            Content = document.Content,
            Embedding = document.Embedding
        };

        await _dbContext.DocumentVectors.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<DocumentVector>> SearchAsync(float[] queryEmbedding, int topK)
    {
        var entities = await _dbContext.DocumentVectors
            .AsNoTracking()
            .ToListAsync();

        var results = entities
            .Select(entity => new
            {
                Document = new DocumentVector
                {
                    DocumentId = entity.DocumentId,
                    ChunkId = entity.ChunkId,
                    Source = entity.Source,
                    Content = entity.Content,
                    Embedding = entity.Embedding
                },
                Similarity = CalculateCosineSimilarity(queryEmbedding, entity.Embedding)
            })
            .OrderByDescending(result => result.Similarity)
            .Take(topK)
            .Select(result => result.Document)
            .ToList();

        return results;
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
