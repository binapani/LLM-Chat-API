namespace LLMChat.Api.Models;

public class AccurateSearchResult
{
    public string DocumentId { get; set; } = string.Empty;
    public int ChunkId { get; set; }
    public string Source { get; set; } = string.Empty;
    public float Similarity { get; set; }
    public string Content { get; set; } = string.Empty;
}
