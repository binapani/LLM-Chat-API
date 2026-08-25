using LLMChat.Api.Data;
using LLMChat.Api.Data.Entities;
using LLMChat.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LLMChat.Api.Services;

public class SQLiteDocumentRepository : IDocumentRepository
{
    private readonly VectorDbContext _dbContext;

    public SQLiteDocumentRepository(VectorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        DocumentMetadata document,
        CancellationToken cancellationToken = default)
    {
        var entity = new DocumentEntity
        {
            Id = document.Id,
            FileName = document.FileName,
            ContentType = document.ContentType,
            UploadedAtUtc = document.UploadedAtUtc,
            Source = document.Source,
            Content = document.Content
        };

        await _dbContext.Set<DocumentEntity>()
            .AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DocumentMetadata?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Set<DocumentEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(document => document.Id == id, cancellationToken);

        return entity is null ? null : ToMetadata(entity);
    }

    public async Task<IReadOnlyList<DocumentMetadata>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Set<DocumentEntity>()
            .AsNoTracking()
            .OrderByDescending(document => document.UploadedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(ToMetadata).ToList();
    }

    public async Task<DocumentMetadata?> UpdateAsync(
        DocumentMetadata document,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Set<DocumentEntity>()
            .FirstOrDefaultAsync(existingDocument => existingDocument.Id == document.Id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.FileName = document.FileName;
        entity.ContentType = document.ContentType;
        entity.UploadedAtUtc = document.UploadedAtUtc;
        entity.Source = document.Source;
        entity.Content = document.Content;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToMetadata(entity);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        var entity = await _dbContext.Set<DocumentEntity>()
            .FindAsync(new object[] { id }, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        var documentId = id.ToString();
        await _dbContext.DocumentVectors
            .Where(vector => vector.DocumentId == documentId)
            .ExecuteDeleteAsync(cancellationToken);

        _dbContext.Set<DocumentEntity>().Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    private static DocumentMetadata ToMetadata(DocumentEntity entity)
    {
        return new DocumentMetadata
        {
            Id = entity.Id,
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            UploadedAtUtc = entity.UploadedAtUtc,
            Source = entity.Source,
            Content = entity.Content
        };
    }
}
