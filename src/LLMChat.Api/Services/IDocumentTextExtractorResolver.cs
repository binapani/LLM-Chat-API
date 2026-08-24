namespace LLMChat.Api.Services;

public interface IDocumentTextExtractorResolver
{
    Task<IDocumentTextExtractor> ResolveAsync(
        string fileName,
        CancellationToken cancellationToken = default);
}
