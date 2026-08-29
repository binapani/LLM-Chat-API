namespace LLMChat.Api.Services;

public interface ISearchKnowledgeBaseTool
{
    Task<string> SearchAsync(string query);
}

public class SearchKnowledgeBaseTool : ISearchKnowledgeBaseTool
{
    private readonly IRAGService _ragService;

    public SearchKnowledgeBaseTool(IRAGService ragService)
    {
        _ragService = ragService;
    }

    public async Task<string> SearchAsync(string query)
    {
        return await _ragService.RetrieveContextAsync(query);
    }
}