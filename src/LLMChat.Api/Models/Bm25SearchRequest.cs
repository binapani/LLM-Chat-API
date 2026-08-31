using System.ComponentModel.DataAnnotations;

namespace LLMChat.Api.Models;

public class Bm25SearchRequest
{
    [Required]
    [MinLength(1)]
    public string Query { get; set; } = string.Empty;

    [Range(1, 20)]
    public int TopK { get; set; } = 5;
}
