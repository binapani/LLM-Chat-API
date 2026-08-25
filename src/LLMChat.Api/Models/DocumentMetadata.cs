namespace LLMChat.Api.Models;

public class DocumentMetadata
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public DateTime UploadedAtUtc { get; set; }

    public string Source { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}
