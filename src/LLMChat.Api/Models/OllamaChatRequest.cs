using System.Text.Json.Serialization;

namespace LLMChat.Api.Models;

public class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OllamaMessage> Messages { get; set; } = new();

    [JsonPropertyName("tools")]
    public List<OllamaTool> Tools { get; set; } = new();

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}