using System.Text;
using UglyToad.PdfPig;

namespace LLMChat.Api.Services;

public class PdfDocumentTextExtractor : IDocumentTextExtractor
{
    public async Task<string> ExtractTextAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (fileStream is null)
        {
            throw new ArgumentNullException(nameof(fileStream));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        var extension = Path.GetExtension(fileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Unsupported file type '{extension}'. Supported type: .pdf.");
        }

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var pdfDocument = PdfDocument.Open(fileStream, new ParsingOptions());
            var pageTexts = new List<string>();

            foreach (var page in pdfDocument.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pageText = page.Text;
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    pageTexts.Add(pageText.Trim());
                }
            }

            return string.Join(Environment.NewLine + Environment.NewLine, pageTexts);
        }, cancellationToken);
    }
}
