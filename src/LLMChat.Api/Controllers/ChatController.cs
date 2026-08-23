using LLMChat.Api.Models;
using LLMChat.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LLMChat.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILLMService _llmService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentIngestionService _documentIngestionService;
    private readonly IVectorStore _vectorStore;
    private readonly IRAGService _ragService;

    public ChatController(
        ILLMService llmService,
        IEmbeddingService embeddingService,
        IDocumentIngestionService documentIngestionService,
        IVectorStore vectorStore,
        IRAGService ragService)
    {
        _llmService = llmService;
        _embeddingService = embeddingService;
        _documentIngestionService = documentIngestionService;
        _vectorStore = vectorStore;
        _ragService = ragService;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var answer = await _llmService.GenerateAnswerAsync(request.Message);

        return Ok(new ChatResponse
        {
            Answer = answer
        });
    }

    [HttpPost("embedding")]
    public async Task<ActionResult<float[]>> GenerateEmbedding([FromBody] string text)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(text);
        return Ok(embedding);
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> IngestDocuments()
    {
        var documents = new[]
        {
            "Password reset policy: Users can reset their password from the account settings page. Passwords must be at least 12 characters long and include a mix of letters, numbers, and symbols.",
            "Annual leave policy: Full-time employees receive twenty days of annual leave per year. Leave requests should be submitted to a manager at least two weeks in advance.",
            "Cafeteria opening hours: The cafeteria is open Monday through Friday from 8:00 AM to 5:00 PM. It is closed on weekends and public holidays."
        };

        await _documentIngestionService.IngestAsync(documents);

        return Ok("Documents ingested successfully.");
    }

    [HttpPost("search")]
    public async Task<ActionResult<IReadOnlyList<DocumentVector>>> Search([FromBody] ChatRequest request)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Message);
        var results = await _vectorStore.SearchAsync(queryEmbedding, 2);

        return Ok(results);
    }

    [HttpPost("rag")]
    public async Task<ActionResult<ChatResponse>> GenerateRagAnswer([FromBody] ChatRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var answer = await _ragService.GenerateAnswerAsync(request.Message);

        return Ok(new ChatResponse
        {
            Answer = answer
        });
    }
}
