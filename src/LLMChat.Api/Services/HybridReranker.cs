using System.Text.RegularExpressions;
using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public class HybridReranker : IReranker
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "is", "a", "an", "of", "to", "in", "for", "and", "on", "what", "how", "does", "do"
    };

    public Task<IReadOnlyList<VectorSearchResult>> RerankAsync(
        string query,
        IReadOnlyList<VectorSearchResult> candidates,
        int topK)
    {
        if (string.IsNullOrWhiteSpace(query) || candidates == null || candidates.Count == 0 || topK <= 0)
        {
            return Task.FromResult<IReadOnlyList<VectorSearchResult>>(Array.Empty<VectorSearchResult>());
        }

        var queryTokens = Tokenize(query);
        var meaningfulQueryTokens = queryTokens
            .Where(token => !StopWords.Contains(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (meaningfulQueryTokens.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<VectorSearchResult>>(Array.Empty<VectorSearchResult>());
        }

        // Semantic similarity tells us whether the candidate is broadly aligned with the query.
        // Keyword overlap adds a lexical signal so that a document containing the query terms
        // is prioritized even when embedding similarity is only moderately strong.
        var reranked = candidates
            .Where(candidate => candidate is not null)
            .Select(candidate =>
            {
                var document = candidate.Document;
                var content = document is not null ? document.Content ?? string.Empty : string.Empty;
                var contentTokens = Tokenize(content);
                var keywordScore = CalculateKeywordOverlapScore(meaningfulQueryTokens, contentTokens);

                // Combine semantic similarity with lexical overlap so the reranker prefers candidates
                // that are both close in embedding space and explicitly mention the query terms.
                var combinedScore = (candidate.Similarity * 0.70f) + (keywordScore * 0.30f);

                return new
                {
                    Candidate = candidate,
                    CombinedScore = combinedScore
                };
            })
            .OrderByDescending(x => x.CombinedScore)
            .Take(topK)
            .Select(x => x.Candidate)
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(reranked);
    }

    private static IReadOnlyList<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var normalized = text.ToLowerInvariant();
        var withoutPunctuation = Regex.Replace(normalized, "[^a-z0-9\\s]", " ");
        var tokens = withoutPunctuation
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .ToArray();

        return tokens;
    }

    private static float CalculateKeywordOverlapScore(
        IReadOnlyList<string> queryTokens,
        IReadOnlyList<string> contentTokens)
    {
        if (queryTokens.Count == 0 || contentTokens.Count == 0)
        {
            return 0f;
        }

        var contentTokenSet = new HashSet<string>(contentTokens, StringComparer.OrdinalIgnoreCase);
        var matches = queryTokens.Count(token => contentTokenSet.Contains(token));

        return (float)matches / queryTokens.Count;
    }
}
