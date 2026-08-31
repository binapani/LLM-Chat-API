using LLMChat.Api.Models;
using LLMChat.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LLMChat.Api.Controllers;

[ApiController]
[Route("api/chat")]
public class Bm25Controller : ControllerBase
{
    private readonly IBm25SearchService _bm25SearchService;

    public Bm25Controller(IBm25SearchService bm25SearchService)
    {
        _bm25SearchService = bm25SearchService;
    }

    [HttpPost("bm25-search")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Bm25SearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Bm25SearchResponse>> Search(
        [FromBody] Bm25SearchRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest("Query is required.");
        }

        if (request.TopK <= 0)
        {
            return BadRequest("topK must be greater than 0.");
        }

        if (request.TopK > 20)
        {
            return BadRequest("topK must be 20 or less.");
        }

        var results = await _bm25SearchService.SearchAsync(
            request.Query,
            request.TopK,
            cancellationToken);

        var response = new Bm25SearchResponse
        {
            Query = request.Query,
            TopK = request.TopK,
            Results = results
                .Select(result => new Bm25SearchResultDto
                {
                    DocumentId = result.DocumentId,
                    ChunkId = result.ChunkId,
                    Source = result.Source,
                    Content = result.Content,
                    Score = result.Score
                })
                .ToList()
        };

        return Ok(response);
    }
}
