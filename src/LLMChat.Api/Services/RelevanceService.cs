namespace LLMChat.Api.Services;

public class RelevanceService : IRelevanceService
{
    private readonly ILLMService _llmService;

    public RelevanceService(ILLMService llmService)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
    }

    public async Task<bool> IsRelevantAsync(string question, string context)
    {
        if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(context))
        {
            return false;
        }

        var prompt = $"You are a document relevance classifier.\n\nDetermine whether the CONTEXT contains information that can directly help answer the QUESTION.\n\nReturn ONLY YES or NO.\n\nQUESTION:\n{question}\n\nCONTEXT:\n{context}";

        var response = await _llmService.GenerateAnswerAsync(prompt);
        var normalized = response?.Trim();

        if (string.Equals(normalized, "YES", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
