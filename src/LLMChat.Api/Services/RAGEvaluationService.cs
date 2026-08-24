using System.Diagnostics;
using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public class RAGEvaluationService : IRAGEvaluationService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IReranker _reranker;
    private readonly IConfiguration _configuration;

    public RAGEvaluationService(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        IReranker reranker,
        IConfiguration configuration)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _reranker = reranker;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<RAGEvaluationResult>> EvaluateAsync(
        IReadOnlyList<RAGEvaluationCase> cases)
    {
        if (cases == null || cases.Count == 0)
        {
            return Array.Empty<RAGEvaluationResult>();
        }

        var minimumSimilarity = _configuration.GetValue<double>("Rag:MinimumSimilarity", 0.50);
        var retrievalTopK = _configuration.GetValue<int>("Rag:RetrievalTopK", 5);
        var relevanceTopK = _configuration.GetValue<int>("Rag:RelevanceTopK", 3);
        var rerankerMinimumScore = _configuration.GetValue<float>("Rag:RerankerMinimumScore", 0.70f);

        var results = new List<RAGEvaluationResult>(cases.Count);

        foreach (var evaluationCase in cases)
        {
            var totalStopwatch = Stopwatch.StartNew();
            var question = evaluationCase?.Question ?? string.Empty;
            var expectedRelevant = evaluationCase?.ExpectedRelevant ?? false;

            if (string.IsNullOrWhiteSpace(question))
            {
                totalStopwatch.Stop();

                results.Add(new RAGEvaluationResult
                {
                    Question = question,
                    ExpectedRelevant = expectedRelevant,
                    TopRerankerScore = null,
                    ActualRelevant = false,
                    IsCorrect = expectedRelevant == false,
                    TotalMs = totalStopwatch.ElapsedMilliseconds
                });

                continue;
            }

            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);
            var retrievalLimit = Math.Max(retrievalTopK, 0);
            var retrievedCandidates = await _vectorStore.SearchAsync(queryEmbedding, retrievalLimit);

            var filteredCandidates = retrievedCandidates
                .Where(candidate => candidate is not null &&
                    candidate.Document != null &&
                    !string.IsNullOrWhiteSpace(candidate.Document.Content) &&
                    candidate.Similarity >= minimumSimilarity)
                .ToList();

            float? topRerankerScore = null;
            var actualRelevant = false;

            if (filteredCandidates.Count > 0)
            {
                var rerankedCandidates = await _reranker.RerankAsync(
                    question,
                    filteredCandidates,
                    Math.Max(relevanceTopK, 0));
    var topCandidate = rerankedCandidates.FirstOrDefault();
                if (rerankedCandidates != null && rerankedCandidates.Count > 0)
                {
                    if (topCandidate != null)
{
    topRerankerScore = topCandidate.Score;
    actualRelevant = topCandidate.Score >= rerankerMinimumScore;
}

                    actualRelevant = topRerankerScore.HasValue &&
                        topRerankerScore.Value >= rerankerMinimumScore;
                }
            }

            totalStopwatch.Stop();

            results.Add(new RAGEvaluationResult
            {
                Question = question,
                ExpectedRelevant = expectedRelevant,
                TopRerankerScore = topRerankerScore,
                ActualRelevant = actualRelevant,
                IsCorrect = expectedRelevant == actualRelevant,
                TotalMs = totalStopwatch.ElapsedMilliseconds
            });
        }

        return results;
    }
}
