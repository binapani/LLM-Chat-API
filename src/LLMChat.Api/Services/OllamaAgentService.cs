using System.Net.Http.Json;
using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public class OllamaAgentService
{
    private const string OllamaEndpoint =
        "http://localhost:11434/api/chat";

    private readonly HttpClient _httpClient;

    public OllamaAgentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OllamaChatResponse> ChatAsync(
        List<OllamaMessage> messages,
        List<OllamaTool> tools,
        CancellationToken cancellationToken = default)
    {
        var request = new OllamaChatRequest
        {
            Model = "qwen2.5:3b",
            Messages = messages,
            Tools = tools,
            Stream = false
        };

        using var response = await _httpClient.PostAsJsonAsync(
            OllamaEndpoint,
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
                cancellationToken: cancellationToken);

        return result ?? new OllamaChatResponse();
    }
}