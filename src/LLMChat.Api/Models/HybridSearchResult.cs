namespace LLMChat.Api.Models;

public class HybridSearchResult
{
    public string DocumentId { get; set; } = string.Empty;

    public int ChunkId { get; set; }

    public string Source { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public float? VectorSimilarity { get; set; }

    public double? Bm25Score { get; set; }

    public bool IsVectorMatch { get; set; }

    public bool IsBm25Match { get; set; }

    public double FinalScore { get; set; }
}
