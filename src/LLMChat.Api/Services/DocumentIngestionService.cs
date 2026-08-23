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

    public async Task IngestAsync(IEnumerable<string> documents)
    {
        foreach (var document in documents)
        {
            var documentId = Guid.NewGuid().ToString();
            var chunks = _documentChunker.Chunk(document, 500, 50);
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
                    Source = "ingestion"
                };

                await _vectorStore.AddAsync(documentVector);
                chunkIndex++;
            }
        }
    }
}