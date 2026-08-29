using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public static class AgentTools
{
    public static OllamaTool SearchKnowledgeBase()
    {
        return new OllamaTool
        {
            Type = "function",
            Function = new OllamaFunction
            {
                Name = "search_knowledge_base",
                Description =
                    "Search the company knowledge base for relevant information from internal documents.",
                Parameters = new OllamaToolParameters
                {
                    Type = "object",
                    Properties = new Dictionary<string, OllamaProperty>
                    {
                        ["query"] = new OllamaProperty
                        {
                            Type = "string",
                            Description =
                                "The search query to use when looking for relevant company information."
                        }
                    },
                    Required = new List<string>
                    {
                        "query"
                    }
                }
            }
        };
    }
    public static OllamaTool Calculator()
{
    return new OllamaTool
    {
        Type = "function",
        Function = new OllamaFunction
        {
            Name = "calculate",
            Description =
                "Perform a mathematical calculation using a valid mathematical expression.",
            Parameters = new OllamaToolParameters
            {
                Type = "object",
                Properties = new Dictionary<string, OllamaProperty>
                {
                    ["expression"] = new OllamaProperty
                    {
                        Type = "string",
                        Description =
                            "A mathematical expression such as 25 * 4 or 100 / 5."
                    }
                },
                Required = new List<string>
                {
                    "expression"
                }
            }
        }
    };
}
}