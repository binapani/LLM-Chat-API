namespace LLMChat.Api.Services;

public interface IDocumentTextExtractor
{
    Task<string> ExtractTextAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);
}
