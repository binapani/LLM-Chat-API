using System.Text.Json.Serialization;

namespace LLMChat.Api.Models;

public class OllamaChatResponse
{
    [JsonPropertyName("message")]
    public OllamaMessage Message { get; set; } = new();

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}