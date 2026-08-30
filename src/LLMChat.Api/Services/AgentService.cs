using System.Diagnostics;
using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public class AgentService : IAgentService
{
    private const string SystemPrompt = """
        You are a helpful enterprise AI assistant.

        Follow this priority order:

        1. CONVERSATION MEMORY
           Previous messages in the current session are trusted conversation context.
           Use this context to answer questions about information the user previously provided,
           such as their name, preferences, goals, or earlier discussion.
           If the answer is already in the conversation history, respond from that information.
           Do not treat personal information from conversation history as company knowledge.

        2. search_knowledge_base
           Use this only when the user asks for company-specific or internal information
           that should come from the company's knowledge base.
           This is not for personal facts already provided in the conversation.
           If the information is already available in the current session memory, do not call
           the knowledge-base tool.

        3. calculate
           Use this for mathematical calculations.

        Guidelines:
        - Answer from conversation memory when it directly addresses the user's question.
        - Do not call search_knowledge_base for information already contained in the conversation.
        - Do not invent company-specific information.
        - If the knowledge base does not contain the requested company information, say that the
          information is not available.
        - Keep the final response natural, concise, and conversational.
        - Do not mention internal tools, tool names, agent iterations, or retrieval mechanics in the
          final response unless the user explicitly asks.
        """;

    private readonly OllamaAgentService _ollamaAgentService;
    private readonly ISearchKnowledgeBaseTool _searchTool;
    private readonly ICalculatorTool _calculatorTool;
    private readonly IConversationMemoryService _conversationMemoryService;
    private readonly ILogger<AgentService> _logger;

    public AgentService(
        OllamaAgentService ollamaAgentService,
        ISearchKnowledgeBaseTool searchTool,
        ICalculatorTool calculatorTool,
        IConversationMemoryService conversationMemoryService,
        ILogger<AgentService> logger)
    {
        _ollamaAgentService = ollamaAgentService;
        _searchTool = searchTool;
        _calculatorTool = calculatorTool;
        _conversationMemoryService = conversationMemoryService;
        _logger = logger;
    }

    public async Task<string> RunAsync(
        string sessionId,
        string userMessage,
        CancellationToken cancellationToken)
    {
        const int maxIterations = 5;

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID is required.", nameof(sessionId));
        }

        var previousMessages = (await _conversationMemoryService.GetMessagesAsync(sessionId)).ToList();
        _logger.LogInformation(
            "Session {SessionId}: {PreviousMessageCount} previous messages loaded.",
            sessionId,
            previousMessages.Count);

        var sessionMessages = new List<OllamaMessage>(previousMessages);
        sessionMessages.Add(new OllamaMessage
        {
            Role = "user",
            Content = userMessage
        });

        var messages = new List<OllamaMessage>
        {
            new OllamaMessage
            {
                Role = "system",
                Content = SystemPrompt
            }
        };
        messages.AddRange(previousMessages);
        messages.Add(new OllamaMessage
        {
            Role = "user",
            Content = userMessage
        });

        var tools = new List<OllamaTool>
        {
            AgentTools.SearchKnowledgeBase(),
            AgentTools.Calculator()
        };

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            _logger.LogInformation(
                "Session {SessionId}: agent iteration {Iteration} started.",
                sessionId,
                iteration + 1);

            var response = await _ollamaAgentService.ChatAsync(
                messages,
                tools,
                cancellationToken);

            var toolCalls = response.Message.ToolCalls;

            if (toolCalls == null || toolCalls.Count == 0)
            {
                _logger.LogInformation(
                    "Session {SessionId}: agent completed on iteration {Iteration}.",
                    sessionId,
                    iteration + 1);

                sessionMessages.Add(response.Message);
                await SaveSessionMessagesAsync(sessionId, sessionMessages, cancellationToken);
                return response.Message.Content;
            }

            sessionMessages.Add(response.Message);
            messages.Add(response.Message);

            var toolResults = await Task.WhenAll(
                toolCalls.Select(async toolCall =>
                {
                    var toolName = toolCall.Function.Name;
                    var stopwatch = Stopwatch.StartNew();

                    _logger.LogInformation(
                        "Session {SessionId}: agent selected tool {ToolName}.",
                        sessionId,
                        toolName);

                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        _logger.LogInformation(
                            "Session {SessionId}: tool execution started for {ToolName}.",
                            sessionId,
                            toolName);

                        var result = await ExecuteToolAsync(
                            toolCall,
                            cancellationToken);

                        stopwatch.Stop();

                        _logger.LogInformation(
                            "Session {SessionId}: tool {ToolName} completed successfully in {DurationMs}ms.",
                            sessionId,
                            toolName,
                            stopwatch.ElapsedMilliseconds);

                        return new OllamaMessage
                        {
                            Role = "tool",
                            Content = result
                        };
                    }
                    catch (OperationCanceledException)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "Session {SessionId}: tool {ToolName} was cancelled after {DurationMs}ms.",
                            sessionId,
                            toolName,
                            stopwatch.ElapsedMilliseconds);

                        throw;
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();

                        _logger.LogError(
                            ex,
                            "Session {SessionId}: tool {ToolName} failed after {DurationMs}ms.",
                            sessionId,
                            toolName,
                            stopwatch.ElapsedMilliseconds);

                        return new OllamaMessage
                        {
                            Role = "tool",
                            Content = $"Tool '{toolName}' failed: {ex.Message}"
                        };
                    }
                }));

            sessionMessages.AddRange(toolResults);
            messages.AddRange(toolResults);
        }

        _logger.LogWarning(
            "Session {SessionId}: agent reached maximum iteration limit of {MaxIterations}.",
            sessionId,
            maxIterations);

        await SaveSessionMessagesAsync(sessionId, sessionMessages, cancellationToken);
        return "The agent reached the maximum number of reasoning steps without producing a final answer.";
    }

    private async Task SaveSessionMessagesAsync(
        string sessionId,
        List<OllamaMessage> sessionMessages,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _conversationMemoryService.ReplaceMessagesAsync(
            sessionId,
            sessionMessages);

        _logger.LogInformation(
            "Session {SessionId}: conversation memory saved with {MessageCount} messages.",
            sessionId,
            sessionMessages.Count);
    }

    private async Task<string> ExecuteToolAsync(
        OllamaToolCall toolCall,
        CancellationToken cancellationToken)
    {
        var toolName = toolCall.Function.Name;

        if (toolName == "search_knowledge_base")
        {
            if (!toolCall.Function.Arguments.TryGetValue(
                    "query",
                    out var queryValue))
            {
                _logger.LogWarning(
                    "Search tool was called without a query.");

                return "The search tool requires a query.";
            }

            var query = queryValue?.ToString();

            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning(
                    "Search tool was called with an empty query.");

                return "The search query cannot be empty.";
            }

            _logger.LogInformation(
                "Executing search_knowledge_base with query: {Query}",
                query);

            cancellationToken.ThrowIfCancellationRequested();

            var toolResult = await _searchTool.SearchAsync(query);
            return toolResult;
        }

        if (toolName == "calculate")
        {
            if (!toolCall.Function.Arguments.TryGetValue(
                    "expression",
                    out var expressionValue))
            {
                _logger.LogWarning(
                    "Calculator was called without an expression.");

                return "The calculator requires an expression.";
            }

            var expression = expressionValue?.ToString();

            if (string.IsNullOrWhiteSpace(expression))
            {
                _logger.LogWarning(
                    "Calculator was called with an empty expression.");

                return "The calculation expression cannot be empty.";
            }

            _logger.LogInformation(
                "Executing calculator with expression: {Expression}",
                expression);

            cancellationToken.ThrowIfCancellationRequested();

            var toolResult = await _calculatorTool.CalculateAsync(expression);
            return toolResult;
        }

        _logger.LogWarning(
            "Unknown tool requested: {ToolName}.",
            toolName);

        return $"Unknown tool: {toolName}. Please use one of the available tools.";
    }
}