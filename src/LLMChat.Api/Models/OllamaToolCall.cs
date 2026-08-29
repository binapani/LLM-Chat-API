using System.Text.Json.Serialization;

namespace LLMChat.Api.Models;

public class OllamaToolCall
{
    [JsonPropertyName("function")]
    public OllamaFunctionCall Function { get; set; } = new();
}

public class OllamaFunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public Dictionary<string, object> Arguments { get; set; } = new();
}