using LLMChat.Api.Models;
using LLMChat.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LLMChat.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILLMService _llmService;

    public ChatController(ILLMService llmService)
    {
        _llmService = llmService;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var answer = await _llmService.GenerateAnswerAsync(request.Message);

        return Ok(new ChatResponse
        {
            Answer = answer
        });
    }
}
