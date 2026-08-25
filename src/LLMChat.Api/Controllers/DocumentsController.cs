using LLMChat.Api.Models;
using LLMChat.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LLMChat.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentTextExtractorResolver _documentTextExtractorResolver;
    private readonly IDocumentIngestionService _documentIngestionService;

    public DocumentsController(
        IDocumentTextExtractorResolver documentTextExtractorResolver,
        IDocumentIngestionService documentIngestionService)
    {
        _documentTextExtractorResolver = documentTextExtractorResolver;
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

        var metadata = new DocumentMetadata
        {
            Id = Guid.NewGuid(),
            FileName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            UploadedAtUtc = DateTime.UtcNow,
            Source = "Upload"
        };

        var extractor = await _documentTextExtractorResolver.ResolveAsync(
            file.FileName,
            cancellationToken);

        await using var stream = file.OpenReadStream();
        var extractedText = await extractor.ExtractTextAsync(
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
            documentId = metadata.Id,
            fileName = metadata.FileName,
            contentType = metadata.ContentType,
            uploadedAtUtc = metadata.UploadedAtUtc,
            source = metadata.Source,
            message = "Document uploaded and ingested successfully.",
            extractedCharacterCount = extractedText.Length
        });
    }
}
