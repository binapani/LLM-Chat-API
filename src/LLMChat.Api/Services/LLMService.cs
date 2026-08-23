using LLMChat.Api.Models;
using System.Net.Http.Json;

namespace LLMChat.Api.Services;

public class LLMService : ILLMService
{
    private const string OllamaEndpoint = "http://localhost:11434/api/generate";
    private readonly HttpClient _httpClient;

    public LLMService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GenerateAnswerAsync(string userMessage)
    {
        var request = new OllamaRequest
        {
            Model = "qwen2.5:3b",
            Prompt = userMessage,
            Stream = false
        };

        using var response = await _httpClient.PostAsJsonAsync(OllamaEndpoint, request);
        response.EnsureSuccessStatusCode();

        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaResponse>();
        return ollamaResponse?.Response ?? string.Empty;
    }
}
