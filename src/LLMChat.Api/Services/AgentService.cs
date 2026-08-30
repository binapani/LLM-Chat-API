using System.Diagnostics;
using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public class AgentService : IAgentService
{
    private const string SystemPrompt = """
        You are a helpful enterprise AI assistant.

        Routing rules:

        1. COMPANY-SPECIFIC FACTS
           If the question asks about company policies, employees, internal documents, company
           procedures, benefits, working hours, annual leave, password policies, cafeteria policies,
           or any other company-specific fact, ALWAYS call search_knowledge_base.
           Do not answer these questions from general knowledge.
           Do not treat personal information from conversation history as company knowledge.

        2. CONVERSATION MEMORY
           If the question can be answered entirely from the current session's conversation history,
           answer from memory and do not call search_knowledge_base.
           Previous messages in the current session are trusted conversation context.
           Use this context for facts the user already provided, such as their name, preferences,
           goals, or earlier discussion.

        3. MATHEMATICS
           If the question is mathematical, ALWAYS use calculate.

        4. COMBINED COMPANY + MATH QUESTIONS
           If a question combines company information and mathematics, use search_knowledge_base
           for the company information and calculate for the mathematical portion.

        5. AFTER TOOL RESULTS
           After receiving tool results, use those results to produce the final answer.
           Never say company information is unavailable if the tool returned evidence containing
           the answer.

        6. FINAL RESPONSE RULES
           Keep the final response natural, concise, and conversational.
           Do not mention tool names, internal reasoning, agent iterations, or retrieval mechanics
           in the final answer unless the user explicitly asks.
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