namespace LLMChat.Api.Services;

public class RAGService : IRAGService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILLMService _llmService;

    public RAGService(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILLMService llmService)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _llmService = llmService;
    }

    public async Task<string> GenerateAnswerAsync(string question)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);
        var documents = await _vectorStore.SearchAsync(queryEmbedding, 2);

        if (documents.Count == 0)
        {
            return "No relevant information was found.";
        }

        var context = string.Join(Environment.NewLine, documents.Select(document => document.Content));
        var prompt = $"Context:\n{context}\n\nQuestion:\n{question}";

        return await _llmService.GenerateAnswerAsync(prompt);
    }
}