namespace LLMChat.Api.Models;

public class Bm25SearchResponse
{
    public string Query { get; set; } = string.Empty;

    public int TopK { get; set; }

    public IReadOnlyList<Bm25SearchResultDto> Results { get; set; } = Array.Empty<Bm25SearchResultDto>();
}

public class Bm25SearchResultDto
{
    public string DocumentId { get; set; } = string.Empty;

    public int ChunkId { get; set; }

    public string Source { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public double Score { get; set; }
}
