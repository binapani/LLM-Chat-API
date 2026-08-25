using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public interface IDocumentRepository
{
    Task AddAsync(
        DocumentMetadata document,
        CancellationToken cancellationToken = default);

    Task<DocumentMetadata?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentMetadata>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
