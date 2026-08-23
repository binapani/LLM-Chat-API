namespace LLMChat.Api.Models;

public class DocumentVector
{
    public string Content { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public string DocumentId { get; set; } = string.Empty;
    public int ChunkId { get; set; } = 0;
    public string Source { get; set; } = string.Empty;
}