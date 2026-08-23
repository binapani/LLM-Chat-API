using LLMChat.Api.Models;
using System.Net.Http.Json;

namespace LLMChat.Api.Services;

public class LLMService : ILLMService
{
    private const string OllamaEndpoint = "http://localhost:11434/api/generate";
    private const string SystemInstruction = "You are an AI instructor for experienced software engineers. Explain technical concepts using practical software engineering examples. Assume the user has professional programming experience. Focus on architecture, implementation, trade-offs, and real-world use cases. Answer in 3-5 sentences unless the user asks for more detail. If you are uncertain, say so rather than inventing information.";
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
            Prompt = $"{SystemInstruction}\n\n{userMessage}",
            Stream = false
        };

        using var response = await _httpClient.PostAsJsonAsync(OllamaEndpoint, request);
        response.EnsureSuccessStatusCode();

        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaResponse>();
        return ollamaResponse?.Response ?? string.Empty;
    }
}
