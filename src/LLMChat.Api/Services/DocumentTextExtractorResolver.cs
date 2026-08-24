namespace LLMChat.Api.Services;

public class DocumentTextExtractorResolver : IDocumentTextExtractorResolver
{
    private readonly PlainTextDocumentExtractor _plainTextDocumentExtractor;
    private readonly PdfDocumentTextExtractor _pdfDocumentTextExtractor;
    private readonly DocxDocumentTextExtractor _docxDocumentTextExtractor;

    public DocumentTextExtractorResolver(
        PlainTextDocumentExtractor plainTextDocumentExtractor,
        PdfDocumentTextExtractor pdfDocumentTextExtractor,
        DocxDocumentTextExtractor docxDocumentTextExtractor)
    {
        _plainTextDocumentExtractor = plainTextDocumentExtractor;
        _pdfDocumentTextExtractor = pdfDocumentTextExtractor;
        _docxDocumentTextExtractor = docxDocumentTextExtractor;
    }

    public Task<IDocumentTextExtractor> ResolveAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        var extension = Path.GetExtension(fileName);

        return extension switch
        {
            ".txt" => Task.FromResult<IDocumentTextExtractor>(_plainTextDocumentExtractor),
            ".md" => Task.FromResult<IDocumentTextExtractor>(_plainTextDocumentExtractor),
            ".csv" => Task.FromResult<IDocumentTextExtractor>(_plainTextDocumentExtractor),
            ".pdf" => Task.FromResult<IDocumentTextExtractor>(_pdfDocumentTextExtractor),
            ".docx" => Task.FromResult<IDocumentTextExtractor>(_docxDocumentTextExtractor),
            _ => throw new NotSupportedException($"Unsupported file type '{extension}'. Supported types: .txt, .md, .csv, .pdf, .docx.")
        };
    }
}
