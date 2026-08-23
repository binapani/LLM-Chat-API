namespace LLMChat.Api.Services;

public interface IDocumentIngestionService
{
    Task IngestAsync(IEnumerable<string> documents);
}