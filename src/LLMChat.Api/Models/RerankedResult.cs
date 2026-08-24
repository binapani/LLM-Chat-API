using LLMChat.Api.Models;

namespace LLMChat.Api.Models;

public class RerankedResult
{
    public VectorSearchResult Result { get; set; } = default!;
    public float Score { get; set; }
}
