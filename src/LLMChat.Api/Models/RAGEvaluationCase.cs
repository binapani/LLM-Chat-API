namespace LLMChat.Api.Models;

public class RAGEvaluationCase
{
    public string Question { get; set; } = string.Empty;

    public bool ExpectedRelevant { get; set; }
}
