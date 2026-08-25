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
    private readonly IDocumentRepository _documentRepository;

    public DocumentsController(
        IDocumentTextExtractorResolver documentTextExtractorResolver,
        IDocumentIngestionService documentIngestionService,
        IDocumentRepository documentRepository)
    {
        _documentTextExtractorResolver = documentTextExtractorResolver;
        _documentIngestionService = documentIngestionService;
        _documentRepository = documentRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var documents = await _documentRepository.GetAllAsync(cancellationToken);

        return Ok(documents);
    }

    [HttpPut("{id:guid}/reindex")]
    public async Task<IActionResult> Reindex(
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(id, cancellationToken);

        if (document is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(document.Content))
        {
            return BadRequest("The document does not contain any content to re-index.");
        }

        await _documentIngestionService.ReindexAsync(
            document.Id,
            document.Source,
            document.Content,
            cancellationToken);

        return Ok(new
        {
            documentId = document.Id,
            message = "Document re-indexed successfully."
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
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

        var existingDocument = await _documentRepository.GetByIdAsync(id, cancellationToken);

        if (existingDocument is null)
        {
            return NotFound();
        }

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

        existingDocument.FileName = file.FileName;
        existingDocument.ContentType = file.ContentType ?? "application/octet-stream";
        existingDocument.UploadedAtUtc = DateTime.UtcNow;
        existingDocument.Source = "Upload";
        existingDocument.Content = extractedText;

        var updatedDocument = await _documentRepository.UpdateAsync(
            existingDocument,
            cancellationToken);

        return updatedDocument is null ? NotFound() : Ok(updatedDocument);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(id, cancellationToken);

        return document is null ? NotFound() : Ok(document);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _documentRepository.DeleteAsync(id, cancellationToken);

        return deleted ? NoContent() : NotFound();
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

        metadata.Content = extractedText;

        await _documentIngestionService.IngestAsync(
            new[] { (file.FileName, extractedText) },
            metadata.Id,
            cancellationToken);

        await _documentRepository.AddAsync(
            metadata,
            cancellationToken);

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
