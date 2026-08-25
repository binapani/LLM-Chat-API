namespace LLMChat.Api.Services;

public interface IDocumentIngestionService
{
    Task IngestAsync(
        IEnumerable<(string Source, string Content)> documents,
        Guid? documentId = null,
        CancellationToken cancellationToken = default);

    Task ReindexAsync(
        Guid documentId,
        string source,
        string content,
        CancellationToken cancellationToken = default);
}