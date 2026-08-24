namespace LLMChat.Api.Models;

public class RAGEvaluationResult
{
    public string Question { get; set; } = string.Empty;

    public bool ExpectedRelevant { get; set; }

    public float? TopRerankerScore { get; set; }

    public bool ActualRelevant { get; set; }

    public bool IsCorrect { get; set; }

    public long TotalMs { get; set; }
}
