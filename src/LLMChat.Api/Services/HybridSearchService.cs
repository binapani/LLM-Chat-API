using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public class HybridSearchService : IHybridSearchService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IBm25SearchService _bm25SearchService;
    private readonly IHybridReranker _hybridReranker;
    private readonly ILogger<HybridSearchService> _logger;

    public HybridSearchService(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        IBm25SearchService bm25SearchService,
        IHybridReranker hybridReranker,
        ILogger<HybridSearchService> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _bm25SearchService = bm25SearchService;
        _hybridReranker = hybridReranker;
        _logger = logger;
    }

    public async Task<IReadOnlyList<HybridSearchResult>> SearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<HybridSearchResult>();
        }

        if (topK <= 0)
        {
            return Array.Empty<HybridSearchResult>();
        }

        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
        var vectorCandidates = await _vectorStore.SearchAsync(queryEmbedding, topK);
        var bm25Candidates = await _bm25SearchService.SearchAsync(query, topK, cancellationToken);

        var merged = MergeCandidates(vectorCandidates, bm25Candidates);

        var reranked = await _hybridReranker.RerankAsync(query, merged, topK);

        _logger.LogInformation(
            "Hybrid search query={Query} vectorCandidateCount={VectorCandidateCount} bm25CandidateCount={Bm25CandidateCount} mergedCandidateCount={MergedCandidateCount} finalResultCount={FinalResultCount}",
            query,
            vectorCandidates.Count,
            bm25Candidates.Count,
            merged.Count,
            reranked.Count);

        return reranked;
    }

    private static List<HybridSearchResult> MergeCandidates(
        IReadOnlyList<VectorSearchResult> vectorCandidates,
        IReadOnlyList<Bm25SearchResult> bm25Candidates)
    {
        var merged = new Dictionary<string, HybridSearchResult>(StringComparer.Ordinal);

        foreach (var candidate in vectorCandidates)
        {
            if (candidate?.Document is null)
            {
                continue;
            }

            var key = GetCandidateKey(candidate.Document.DocumentId, candidate.Document.ChunkId);
            if (!merged.TryGetValue(key, out var existing))
            {
                merged[key] = new HybridSearchResult
                {
                    DocumentId = candidate.Document.DocumentId,
                    ChunkId = candidate.Document.ChunkId,
                    Source = candidate.Document.Source,
                    Content = candidate.Document.Content,
                    VectorSimilarity = candidate.Similarity,
                    IsVectorMatch = true
                };
                continue;
            }

            existing.VectorSimilarity = candidate.Similarity;
            existing.IsVectorMatch = true;
            existing.Source = string.IsNullOrWhiteSpace(existing.Source) ? candidate.Document.Source : existing.Source;
            existing.Content = string.IsNullOrWhiteSpace(existing.Content) ? candidate.Document.Content : existing.Content;
        }

        foreach (var candidate in bm25Candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.DocumentId))
            {
                continue;
            }

            var key = GetCandidateKey(candidate.DocumentId, candidate.ChunkId);
            if (!merged.TryGetValue(key, out var existing))
            {
                merged[key] = new HybridSearchResult
                {
                    DocumentId = candidate.DocumentId,
                    ChunkId = candidate.ChunkId,
                    Source = candidate.Source,
                    Content = candidate.Content,
                    Bm25Score = candidate.Score,
                    IsBm25Match = true
                };
                continue;
            }

            existing.Bm25Score = candidate.Score;
            existing.IsBm25Match = true;
            existing.Source = string.IsNullOrWhiteSpace(existing.Source) ? candidate.Source : existing.Source;
            existing.Content = string.IsNullOrWhiteSpace(existing.Content) ? candidate.Content : existing.Content;
        }

        return merged.Values
            .OrderByDescending(x => x.IsVectorMatch ? x.VectorSimilarity ?? 0f : 0f)
            .ThenByDescending(x => x.IsBm25Match ? x.Bm25Score ?? 0d : 0d)
            .ToList();
    }

    private static string GetCandidateKey(string documentId, int chunkId)
    {
        return $"{documentId}:{chunkId}";
    }
}
