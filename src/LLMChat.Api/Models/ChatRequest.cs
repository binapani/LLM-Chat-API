using System.ComponentModel.DataAnnotations;

namespace LLMChat.Api.Models;

public class ChatRequest
{
    [Required]
    public string Message { get; set; } = string.Empty;
}
