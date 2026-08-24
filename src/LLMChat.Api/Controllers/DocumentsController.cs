using LLMChat.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LLMChat.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentTextExtractor _documentTextExtractor;
    private readonly IDocumentIngestionService _documentIngestionService;

    public DocumentsController(
        IDocumentTextExtractor documentTextExtractor,
        IDocumentIngestionService documentIngestionService)
    {
        _documentTextExtractor = documentTextExtractor;
        _documentIngestionService = documentIngestionService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest("File is required.");
        }

        if (file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        await using var stream = file.OpenReadStream();
        var extractedText = await _documentTextExtractor.ExtractTextAsync(
            stream,
            file.FileName,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return BadRequest("The document does not contain any text.");
        }

        await _documentIngestionService.IngestAsync(
            new[] { (file.FileName, extractedText) });

        return Ok(new
        {
            fileName = file.FileName,
            message = "Document uploaded and ingested successfully.",
            extractedCharacterCount = extractedText.Length
        });
    }
}
