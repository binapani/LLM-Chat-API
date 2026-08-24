using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public class DocumentIngestionService : IDocumentIngestionService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IDocumentChunker _documentChunker;

    public DocumentIngestionService(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        IDocumentChunker documentChunker)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _documentChunker = documentChunker;
    }

    public async Task IngestAsync(IEnumerable<(string Source, string Content)> documents)
    {
        foreach (var document in documents)
        {
            var documentId = Guid.NewGuid().ToString();
            var source = string.IsNullOrWhiteSpace(document.Source) ? "unknown" : document.Source;
            var chunks = _documentChunker.Chunk(document.Content, 500, 50);
            var chunkIndex = 0;

            foreach (var chunk in chunks)
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk);
                var documentVector = new DocumentVector
                {
                    DocumentId = documentId,
                    ChunkId = chunkIndex,
                    Content = chunk,
                    Embedding = embedding,
                    Source = source
                };

                await _vectorStore.AddAsync(documentVector);
                chunkIndex++;
            }
        }
    }
}