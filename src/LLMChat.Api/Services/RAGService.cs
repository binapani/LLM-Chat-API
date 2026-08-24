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
        var documents = await _vectorStore.SearchAsync(queryEmbedding, 3);

        if (documents.Count == 0)
        {
            return "The information is not available in the provided documents.";
        }

        var context = string.Join(
            Environment.NewLine + Environment.NewLine,
            documents.Select(result => result.Document.Content));

        var prompt = $@"You are an enterprise assistant.
Answer the user's question using ONLY the information provided in the context.
If the answer cannot be found in the context, say that the information is not available in the provided documents.
Do not invent or assume information.

Context:
{context}

User question:
{question}";

        return await _llmService.GenerateAnswerAsync(prompt);
    }
}