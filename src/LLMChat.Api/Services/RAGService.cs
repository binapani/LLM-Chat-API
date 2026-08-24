using System.Diagnostics;
using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public class RAGService : IRAGService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILLMService _llmService;
    private readonly IRelevanceService _relevanceService;
    private readonly IReranker _reranker;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RAGService> _logger;

    public RAGService(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILLMService llmService,
        IRelevanceService relevanceService,
        IReranker reranker,
        IConfiguration configuration,
        ILogger<RAGService> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _llmService = llmService;
        _relevanceService = relevanceService;
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

        var candidates = retrievedCandidates
            .Where(candidate => candidate.Document != null &&
                !string.IsNullOrWhiteSpace(candidate.Document.Content) &&
                candidate.Similarity >= minimumSimilarity)
            .OrderByDescending(candidate => candidate.Similarity)
            .ToList();

        var candidatesAfterSimilarityFilter = candidates.Count;

        if (candidatesAfterSimilarityFilter == 0)
        {
            totalStopwatch.Stop();
            var earlyTotalMs = totalStopwatch.ElapsedMilliseconds;
            _logger.LogInformation(
                "RAG completed. TotalMs={TotalMs}, EmbeddingMs={EmbeddingMs}, VectorSearchMs={VectorSearchMs}, RelevanceValidationMs={RelevanceValidationMs}, FinalLlmMs={FinalLlmMs}, RerankingMs={RerankingMs}, RetrievalTopK={RetrievalTopK}, RelevanceTopK={RelevanceTopK}, CandidatesRetrieved={CandidatesRetrieved}, CandidatesAfterSimilarityFilter={CandidatesAfterSimilarityFilter}, CandidatesValidated={CandidatesValidated}, RelevantCandidates={RelevantCandidates}",
                earlyTotalMs,
                embeddingMs,
                vectorSearchMs,
                0,
                0,
                0,
                retrievalTopK,
                relevanceTopK,
                candidatesRetrieved,
                candidatesAfterSimilarityFilter,
                0,
                0);

            return "The information is not available in the provided documents.";
        }

        var rerankingStopwatch = Stopwatch.StartNew();
        var rerankedCandidates = await _reranker.RerankAsync(question, candidates, relevanceTopK);
        rerankingStopwatch.Stop();
        var rerankingMs = rerankingStopwatch.ElapsedMilliseconds;

        var relevanceValidationStopwatch = Stopwatch.StartNew();
        var relevanceChecks = rerankedCandidates
            .Select(candidate => new
            {
                Candidate = candidate,
                IsRelevantTask = _relevanceService.IsRelevantAsync(question, candidate.Document.Content)
            })
            .ToList();

        var relevanceResults = await Task.WhenAll(relevanceChecks.Select(item => item.IsRelevantTask));
        var relevantCandidates = new List<VectorSearchResult>();

        for (var i = 0; i < relevanceChecks.Count; i++)
        {
            if (relevanceResults[i])
            {
                relevantCandidates.Add(relevanceChecks[i].Candidate);
            }
        }

        var candidatesValidated = relevanceChecks.Count;
        relevanceValidationStopwatch.Stop();
        var relevanceValidationMs = relevanceValidationStopwatch.ElapsedMilliseconds;

        if (relevantCandidates.Count == 0)
        {
            totalStopwatch.Stop();
            var noRelevantTotalMs = totalStopwatch.ElapsedMilliseconds;
            _logger.LogInformation(
                "RAG completed. TotalMs={TotalMs}, EmbeddingMs={EmbeddingMs}, VectorSearchMs={VectorSearchMs}, RelevanceValidationMs={RelevanceValidationMs}, FinalLlmMs={FinalLlmMs}, RerankingMs={RerankingMs}, RetrievalTopK={RetrievalTopK}, RelevanceTopK={RelevanceTopK}, CandidatesRetrieved={CandidatesRetrieved}, CandidatesAfterSimilarityFilter={CandidatesAfterSimilarityFilter}, CandidatesValidated={CandidatesValidated}, RelevantCandidates={RelevantCandidates}",
                noRelevantTotalMs,
                embeddingMs,
                vectorSearchMs,
                relevanceValidationMs,
                0,
                rerankingMs,
                retrievalTopK,
                relevanceTopK,
                candidatesRetrieved,
                candidatesAfterSimilarityFilter,
                candidatesValidated,
                relevantCandidates.Count);

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

        var finalLlmStopwatch = Stopwatch.StartNew();
        var finalAnswer = await _llmService.GenerateAnswerAsync(prompt);
        finalLlmStopwatch.Stop();
        var finalLlmMs = finalLlmStopwatch.ElapsedMilliseconds;
        totalStopwatch.Stop();
        var totalMs = totalStopwatch.ElapsedMilliseconds;

        _logger.LogInformation(
            "RAG completed. TotalMs={TotalMs}, EmbeddingMs={EmbeddingMs}, VectorSearchMs={VectorSearchMs}, RelevanceValidationMs={RelevanceValidationMs}, FinalLlmMs={FinalLlmMs}, RerankingMs={RerankingMs}, RetrievalTopK={RetrievalTopK}, RelevanceTopK={RelevanceTopK}, CandidatesRetrieved={CandidatesRetrieved}, CandidatesAfterSimilarityFilter={CandidatesAfterSimilarityFilter}, CandidatesValidated={CandidatesValidated}, RelevantCandidates={RelevantCandidates}",
            totalMs,
            embeddingMs,
            vectorSearchMs,
            relevanceValidationMs,
            finalLlmMs,
            rerankingMs,
            retrievalTopK,
            relevanceTopK,
            candidatesRetrieved,
            candidatesAfterSimilarityFilter,
            candidatesValidated,
            relevantCandidates.Count);

        return finalAnswer;
    }
}