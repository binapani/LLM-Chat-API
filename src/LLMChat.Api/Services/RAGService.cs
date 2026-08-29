using System.Diagnostics;
using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public class RAGService : IRAGService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILLMService _llmService;
    private readonly IReranker _reranker;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RAGService> _logger;

    public RAGService(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILLMService llmService,
        IReranker reranker,
        IConfiguration configuration,
        ILogger<RAGService> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _llmService = llmService;
        _reranker = reranker;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GenerateAnswerAsync(string question)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var embeddingStopwatch = Stopwatch.StartNew();

        var minimumSimilarity = _configuration.GetValue<double>("Rag:MinimumSimilarity", 0.50);
        var retrievalTopK = _configuration.GetValue<int>("Rag:RetrievalTopK", 5);
        var relevanceTopK = _configuration.GetValue<int>("Rag:RelevanceTopK", 3);

        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);
        embeddingStopwatch.Stop();
        var embeddingMs = embeddingStopwatch.ElapsedMilliseconds;

        var vectorSearchStopwatch = Stopwatch.StartNew();
        var retrievedCandidates = await _vectorStore.SearchAsync(queryEmbedding, retrievalTopK);
        vectorSearchStopwatch.Stop();
        var vectorSearchMs = vectorSearchStopwatch.ElapsedMilliseconds;
        var candidatesRetrieved = retrievedCandidates.Count;

        var filteredCandidates = retrievedCandidates
            .Where(candidate => candidate.Document != null &&
                !string.IsNullOrWhiteSpace(candidate.Document.Content) &&
                candidate.Similarity >= minimumSimilarity)
            .OrderByDescending(candidate => candidate.Similarity)
            .ToList();

        var candidatesAfterSimilarityFilter = filteredCandidates.Count;

        if (candidatesAfterSimilarityFilter == 0)
        {
            totalStopwatch.Stop();
            var earlyTotalMs = totalStopwatch.ElapsedMilliseconds;
            _logger.LogInformation(
                "RAG completed. TotalMs={TotalMs}, EmbeddingMs={EmbeddingMs}, VectorSearchMs={VectorSearchMs}, FinalLlmMs={FinalLlmMs}, RerankingMs={RerankingMs}, TopRerankerScore={TopRerankerScore}, RetrievalTopK={RetrievalTopK}, RelevanceTopK={RelevanceTopK}, CandidatesRetrieved={CandidatesRetrieved}, CandidatesAfterSimilarityFilter={CandidatesAfterSimilarityFilter}, CandidatesAfterReranking={CandidatesAfterReranking}",
                earlyTotalMs,
                embeddingMs,
                vectorSearchMs,
                0,
                0,
                0f,
                retrievalTopK,
                relevanceTopK,
                candidatesRetrieved,
                candidatesAfterSimilarityFilter,
                0);

            return "The information is not available in the provided documents.";
        }

        var rerankingStopwatch = Stopwatch.StartNew();
        var rerankedCandidates = await _reranker.RerankAsync(question, filteredCandidates, relevanceTopK);
        rerankingStopwatch.Stop();
        var rerankingMs = rerankingStopwatch.ElapsedMilliseconds;

        var candidatesAfterReranking = rerankedCandidates.Count;
        var topRerankerScore = rerankedCandidates.Count > 0 ? rerankedCandidates[0].Score : 0f;

        if (candidatesAfterReranking == 0)
        {
            totalStopwatch.Stop();
            var noRerankedTotalMs = totalStopwatch.ElapsedMilliseconds;
            _logger.LogInformation(
                "RAG completed. TotalMs={TotalMs}, EmbeddingMs={EmbeddingMs}, VectorSearchMs={VectorSearchMs}, FinalLlmMs={FinalLlmMs}, RerankingMs={RerankingMs}, TopRerankerScore={TopRerankerScore}, RetrievalTopK={RetrievalTopK}, RelevanceTopK={RelevanceTopK}, CandidatesRetrieved={CandidatesRetrieved}, CandidatesAfterSimilarityFilter={CandidatesAfterSimilarityFilter}, CandidatesAfterReranking={CandidatesAfterReranking}",
                noRerankedTotalMs,
                embeddingMs,
                vectorSearchMs,
                0,
                rerankingMs,
                topRerankerScore,
                retrievalTopK,
                relevanceTopK,
                candidatesRetrieved,
                candidatesAfterSimilarityFilter,
                candidatesAfterReranking);

            return "The information is not available in the provided documents.";
        }

        var finalContextCandidates = rerankedCandidates
            .Take(relevanceTopK)
            .Select(result => result.Result)
            .ToList();

        var context = string.Join(
            Environment.NewLine + Environment.NewLine,
            finalContextCandidates.Select(result => result.Document.Content));

        var prompt = $@"You are an enterprise assistant.
Answer the user's question using ONLY the information provided in the context.
If the answer cannot be found in the context, say that the information is not available in the provided documents.
Do not invent or assume information.

Context:
{context}

User question:
{question}";

        var finalLlmStopwatch = Stopwatch.StartNew();
        var finalAnswer = await _llmService.GenerateAnswerAsync(prompt);
        finalLlmStopwatch.Stop();
        var finalLlmMs = finalLlmStopwatch.ElapsedMilliseconds;
        totalStopwatch.Stop();
        var totalMs = totalStopwatch.ElapsedMilliseconds;

        _logger.LogInformation(
            "RAG completed. TotalMs={TotalMs}, EmbeddingMs={EmbeddingMs}, VectorSearchMs={VectorSearchMs}, FinalLlmMs={FinalLlmMs}, RerankingMs={RerankingMs}, TopRerankerScore={TopRerankerScore}, RetrievalTopK={RetrievalTopK}, RelevanceTopK={RelevanceTopK}, CandidatesRetrieved={CandidatesRetrieved}, CandidatesAfterSimilarityFilter={CandidatesAfterSimilarityFilter}, CandidatesAfterReranking={CandidatesAfterReranking}",
            totalMs,
            embeddingMs,
            vectorSearchMs,
            finalLlmMs,
            rerankingMs,
            topRerankerScore,
            retrievalTopK,
            relevanceTopK,
            candidatesRetrieved,
            candidatesAfterSimilarityFilter,
            candidatesAfterReranking);

        return finalAnswer;
    }
    public async Task<string> RetrieveContextAsync(string question)
{
    var minimumSimilarity =
        _configuration.GetValue<double>("Rag:MinimumSimilarity", 0.50);

    var retrievalTopK =
        _configuration.GetValue<int>("Rag:RetrievalTopK", 5);

    var relevanceTopK =
        _configuration.GetValue<int>("Rag:RelevanceTopK", 3);

    var queryEmbedding =
        await _embeddingService.GenerateEmbeddingAsync(question);

    var retrievedCandidates =
        await _vectorStore.SearchAsync(queryEmbedding, retrievalTopK);

    var filteredCandidates = retrievedCandidates
        .Where(candidate =>
            candidate.Document != null &&
            !string.IsNullOrWhiteSpace(candidate.Document.Content) &&
            candidate.Similarity >= minimumSimilarity)
        .OrderByDescending(candidate => candidate.Similarity)
        .ToList();

    if (filteredCandidates.Count == 0)
    {
        return "The information is not available in the provided documents.";
    }

    var rerankedCandidates =
        await _reranker.RerankAsync(
            question,
            filteredCandidates,
            relevanceTopK);

    if (rerankedCandidates.Count == 0)
    {
        return "The information is not available in the provided documents.";
    }

    var context = string.Join(
        Environment.NewLine + Environment.NewLine,
        rerankedCandidates
            .Take(relevanceTopK)
            .Select(result => result.Result.Document.Content));

    return context;
}
}