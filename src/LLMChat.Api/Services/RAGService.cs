using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public class RAGService : IRAGService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILLMService _llmService;
    private readonly IRelevanceService _relevanceService;

    public RAGService(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILLMService llmService,
        IRelevanceService relevanceService)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _llmService = llmService;
        _relevanceService = relevanceService;
    }

    public async Task<string> GenerateAnswerAsync(string question)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);
        var candidates = await _vectorStore.SearchAsync(queryEmbedding, 5);

        if (candidates.Count == 0)
        {
            return "The information is not available in the provided documents.";
        }

        var relevantCandidates = new List<VectorSearchResult>();

        foreach (var candidate in candidates)
        {
            var content = candidate.Document?.Content;

            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            var isRelevant = await _relevanceService.IsRelevantAsync(question, content);

            if (isRelevant)
            {
                relevantCandidates.Add(candidate);
            }
        }

        if (relevantCandidates.Count == 0)
        {
            return "The information is not available in the provided documents.";
        }

        var context = string.Join(
            Environment.NewLine + Environment.NewLine,
            relevantCandidates.Select(result => result.Document.Content));

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