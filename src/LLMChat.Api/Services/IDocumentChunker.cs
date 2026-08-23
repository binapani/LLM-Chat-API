namespace LLMChat.Api.Services;

public interface IDocumentChunker
{
    IEnumerable<string> Chunk(string document, int chunkSize, int overlap);
}
