using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LLMChat.Api.Data.Entities;

[Table("ConversationMessages")]
public class ConversationMessageEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ConversationSessionId { get; set; }

    [Required]
    public int SequenceNumber { get; set; }

    [Required]
    [MaxLength(32)]
    public string Role { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "TEXT")]
    public string Content { get; set; } = string.Empty;

    [Column(TypeName = "TEXT")]
    public string? ToolCallsJson { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ConversationSessionEntity ConversationSession { get; set; } = null!;
}
