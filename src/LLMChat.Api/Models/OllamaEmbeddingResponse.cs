using System.Text.Json.Serialization;

namespace LLMChat.Api.Models;

public class OllamaEmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = Array.Empty<float>();
}