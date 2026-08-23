using LLMChat.Api.Models;
using System.Net.Http.Json;

namespace LLMChat.Api.Services;

public class EmbeddingService : IEmbeddingService
{
    private const string OllamaEndpoint = "http://localhost:11434/api/embeddings";
    private readonly HttpClient _httpClient;

    public EmbeddingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var request = new OllamaEmbeddingRequest
        {
            Model = "nomic-embed-text",
            Prompt = text
        };

        using var response = await _httpClient.PostAsJsonAsync(OllamaEndpoint, request);
        response.EnsureSuccessStatusCode();

        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
        return ollamaResponse?.Embedding ?? Array.Empty<float>();
    }
}