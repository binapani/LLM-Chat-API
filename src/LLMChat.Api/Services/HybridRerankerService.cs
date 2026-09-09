using System.Text.RegularExpressions;
using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public class HybridRerankerService : IHybridReranker
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "is", "a", "an", "of", "to", "in", "for", "and", "on", "what", "how", "does", "do",
        "with", "from", "by", "as", "at", "it", "be", "are", "many", "much"
    };

    public Task<IReadOnlyList<HybridSearchResult>> RerankAsync(
        string query,
        IReadOnlyList<HybridSearchResult> candidates,
        int topK)
    {
        if (string.IsNullOrWhiteSpace(query)
            || candidates is null
            || candidates.Count == 0
            || topK <= 0)
        {
            return Task.FromResult<IReadOnlyList<HybridSearchResult>>(Array.Empty<HybridSearchResult>());
        }

        var queryTokens = Tokenize(query);
        var meaningfulQueryTokens = queryTokens
            .Where(token => !StopWords.Contains(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (meaningfulQueryTokens.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<HybridSearchResult>>(Array.Empty<HybridSearchResult>());
        }

        var reranked = candidates
            .Select(candidate =>
            {
                var vectorScore = NormalizeVectorScore(candidate.VectorSimilarity);
                var bm25Score = NormalizeBm25Score(candidate.Bm25Score);
                var keywordOverlap = CalculateKeywordOverlapScore(meaningfulQueryTokens, Tokenize(candidate.Content));

                var score = (vectorScore * 0.50d)
                    + (bm25Score * 0.30d)
                    + (keywordOverlap * 0.20d);

                return new HybridSearchResult
                {
                    DocumentId = candidate.DocumentId,
                    ChunkId = candidate.ChunkId,
                    Source = candidate.Source,
                    Content = candidate.Content,
                    VectorSimilarity = candidate.VectorSimilarity,
                    Bm25Score = candidate.Bm25Score,
                    IsVectorMatch = candidate.IsVectorMatch,
                    IsBm25Match = candidate.IsBm25Match,
                    FinalScore = score
                };
            })
            .OrderByDescending(x => x.FinalScore)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<HybridSearchResult>>(reranked);
    }

    private static double NormalizeVectorScore(float? similarity)
    {
        if (similarity is null)
        {
            return 0d;
        }

        return Math.Clamp(similarity.Value, -1d, 1d);
    }

    private static double NormalizeBm25Score(double? bm25Score)
    {
        if (bm25Score is null)
        {
            return 0d;
        }

        var score = bm25Score.Value;

        if (double.IsNaN(score) || double.IsInfinity(score))
        {
            return 0d;
        }

        // SQLite FTS5 bm25() is negative for stronger matches (e.g., -0.5, -3.2), so we
        // convert it to a positive normalized signal where higher is better.
        var normalized = -score;
        return Math.Clamp(normalized, 0d, 100d);
    }

    private static double CalculateKeywordOverlapScore(
        IReadOnlyList<string> queryTokens,
        IReadOnlyList<string> contentTokens)
    {
        if (queryTokens.Count == 0 || contentTokens.Count == 0)
        {
            return 0d;
        }

        var contentTokenSet = new HashSet<string>(contentTokens, StringComparer.OrdinalIgnoreCase);
        var matches = queryTokens.Count(token => contentTokenSet.Contains(token));

        return (double)matches / queryTokens.Count;
    }

    private static IReadOnlyList<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var normalized = text.ToLowerInvariant();
        var withoutPunctuation = Regex.Replace(normalized, "[^a-z0-9\\s]", " ");
        return withoutPunctuation
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .ToArray();
    }
}
