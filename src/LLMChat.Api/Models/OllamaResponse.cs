using System.Text.Json.Serialization;

namespace LLMChat.Api.Models;

public class OllamaResponse
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}