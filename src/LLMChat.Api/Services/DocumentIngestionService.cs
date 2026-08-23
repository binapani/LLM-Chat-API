using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public class DocumentIngestionService : IDocumentIngestionService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;

    public DocumentIngestionService(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
    }

    public async Task IngestAsync(IEnumerable<string> documents)
    {
        foreach (var document in documents)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(document);
            var documentVector = new DocumentVector
            {
                Content = document,
                Embedding = embedding
            };

            await _vectorStore.AddAsync(documentVector);
        }
    }
}