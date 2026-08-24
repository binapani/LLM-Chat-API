namespace LLMChat.Api.Models;

public class VectorSearchResult
{
    public DocumentVector Document { get; set; } = new();
    public float Similarity { get; set; }
}
