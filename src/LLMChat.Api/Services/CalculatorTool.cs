namespace LLMChat.Api.Services;

public interface ICalculatorTool
{
    Task<string> CalculateAsync(string expression);
}

public class CalculatorTool : ICalculatorTool
{
    public Task<string> CalculateAsync(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return Task.FromResult("The expression cannot be empty.");
        }

        try
        {
            var result = new System.Data.DataTable()
                .Compute(expression, null);

            return Task.FromResult(result?.ToString() ?? "No result.");
        }
        catch
        {
            return Task.FromResult(
                "Unable to calculate the provided expression.");
        }
    }
}