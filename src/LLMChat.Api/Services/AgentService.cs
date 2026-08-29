using System.Diagnostics;
using LLMChat.Api.Models;

namespace LLMChat.Api.Services;

public class AgentService : IAgentService
{
    private readonly OllamaAgentService _ollamaAgentService;
    private readonly ISearchKnowledgeBaseTool _searchTool;
    private readonly ICalculatorTool _calculatorTool;
    private readonly ILogger<AgentService> _logger;

    public AgentService(
        OllamaAgentService ollamaAgentService,
        ISearchKnowledgeBaseTool searchTool,
        ICalculatorTool calculatorTool,
        ILogger<AgentService> logger)
    {
        _ollamaAgentService = ollamaAgentService;
        _searchTool = searchTool;
        _calculatorTool = calculatorTool;
        _logger = logger;
    }

    public async Task<string> RunAsync(
        string userMessage,
        CancellationToken cancellationToken)
    {
        const int maxIterations = 5;

        var messages = new List<OllamaMessage>
        {
            new OllamaMessage
            {
                Role = "system",
                Content = """
          You are an enterprise AI assistant.

          You have access to two tools:

          1. search_knowledge_base
             Use this for company-specific information
             contained in internal documents.

          2. calculate
             Use this for mathematical calculations.

          Decide which tool or tools are required to answer
          the user's question.

          Company information returned by search_knowledge_base
          is the authoritative source for company-specific facts.

          After receiving tool results, use the information
          contained in those results to answer the user.

          Do not claim that information is unavailable if the
          search tool returned evidence that answers the question.

          Do not invent company-specific information.

          If the available tool results do not contain the
          requested company information, clearly say that the
          information is not available.

          Provide a concise final answer.
          """
            },
            new OllamaMessage
            {
                Role = "user",
                Content = userMessage
            }
        };

        var tools = new List<OllamaTool>
        {
            AgentTools.SearchKnowledgeBase(),
            AgentTools.Calculator()
        };

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            _logger.LogInformation(
                "Agent iteration {Iteration} started.",
                iteration + 1);

            var response = await _ollamaAgentService.ChatAsync(
                messages,
                tools,
                cancellationToken);

            var toolCalls = response.Message.ToolCalls;

            if (toolCalls == null || toolCalls.Count == 0)
            {
                _logger.LogInformation(
                    "Agent completed on iteration {Iteration}.",
                    iteration + 1);

                return response.Message.Content;
            }

            // Preserve the assistant's tool-call message.
            messages.Add(response.Message);

            var toolResults = await Task.WhenAll(
                toolCalls.Select(async toolCall =>
                {
                    var toolName = toolCall.Function.Name;
                    var stopwatch = Stopwatch.StartNew();

                    _logger.LogInformation(
                        "Agent selected tool {ToolName}.",
                        toolName);

                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        _logger.LogInformation(
                            "Tool execution started for {ToolName}.",
                            toolName);

                        var result = await ExecuteToolAsync(
                            toolCall,
                            cancellationToken);

                        stopwatch.Stop();

                        _logger.LogInformation(
                            "Tool {ToolName} completed successfully in {DurationMs}ms.",
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
                            "Tool {ToolName} was cancelled after {DurationMs}ms.",
                            toolName,
                            stopwatch.ElapsedMilliseconds);

                        throw;
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();

                        _logger.LogError(
                            ex,
                            "Tool {ToolName} failed after {DurationMs}ms.",
                            toolName,
                            stopwatch.ElapsedMilliseconds);

                        return new OllamaMessage
                        {
                            Role = "tool",
                            Content = $"Tool '{toolName}' failed: {ex.Message}"
                        };
                    }
                }));

            messages.AddRange(toolResults);
        }

        _logger.LogWarning(
            "Agent reached maximum iteration limit of {MaxIterations}.",
            maxIterations);

        return "The agent reached the maximum number of reasoning steps without producing a final answer.";
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