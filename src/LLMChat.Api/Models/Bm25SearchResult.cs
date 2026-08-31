namespace LLMChat.Api.Models;

public class Bm25SearchResult
{
    public string DocumentId { get; set; } = string.Empty;

    public int ChunkId { get; set; }

    public string Source { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public double Score { get; set; }
}
