using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace LLMChat.Api.Services;

public class DocxDocumentTextExtractor : IDocumentTextExtractor
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
        if (!string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Unsupported file type '{extension}'. Supported type: .docx.");
        }

        try
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var textBuilder = new StringBuilder();

                using var document = WordprocessingDocument.Open(fileStream, false, new OpenSettings
                {
                    AutoSave = false
                });

                var body = document.MainDocumentPart?.Document?.Body;
                if (body is null)
                {
                    return string.Empty;
                }

                AppendParagraphs(body, textBuilder, cancellationToken);
                AppendTables(body, textBuilder, cancellationToken);

                return textBuilder.ToString();
            }, cancellationToken);
        }
        catch (OpenXmlPackageException ex)
        {
            throw new InvalidOperationException("The DOCX file is invalid or corrupted and could not be read.", ex);
        }
    }

    private static void AppendParagraphs(
        Body body,
        StringBuilder textBuilder,
        CancellationToken cancellationToken)
    {
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var paragraphText = paragraph.InnerText;
            if (!string.IsNullOrWhiteSpace(paragraphText))
            {
                textBuilder.AppendLine(paragraphText.Trim());
            }
        }
    }

    private static void AppendTables(
        Body body,
        StringBuilder textBuilder,
        CancellationToken cancellationToken)
    {
        foreach (var table in body.Elements<Table>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowText = new List<string>();

            foreach (var row in table.Elements<TableRow>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cells = row.Elements<TableCell>()
                    .Select(cell => cell.InnerText.Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToList();

                if (cells.Count > 0)
                {
                    rowText.Add(string.Join(" | ", cells));
                }
            }

            if (rowText.Count > 0)
            {
                textBuilder.AppendLine(string.Join(Environment.NewLine, rowText));
            }
        }
    }
}
