namespace LLMChat.Api.Models;

public class DocumentVector
{
    public string Content { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
}