namespace LLMChat.Api.Services;

public class DocumentChunker : IDocumentChunker
{
    public IEnumerable<string> Chunk(string document, int chunkSize, int overlap)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (chunkSize <= 0)
        {
            throw new ArgumentException("chunkSize must be greater than 0.", nameof(chunkSize));
        }

        if (overlap < 0 || overlap >= chunkSize)
        {
            throw new ArgumentException("overlap must be greater than or equal to 0 and less than chunkSize.", nameof(overlap));
        }

        if (document.Length == 0)
        {
            return Enumerable.Empty<string>();
        }

        var chunks = new List<string>();
        var step = chunkSize - overlap;

        for (var start = 0; start < document.Length; start += step)
        {
            var end = Math.Min(start + chunkSize, document.Length);
            chunks.Add(document.Substring(start, end - start));

            if (end == document.Length)
            {
                break;
            }
        }

        return chunks;
    }
}
