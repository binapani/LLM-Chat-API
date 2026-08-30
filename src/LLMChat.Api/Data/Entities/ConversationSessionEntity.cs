using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LLMChat.Api.Data.Entities;

[Table("ConversationSessions")]
public class ConversationSessionEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string SessionId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<ConversationMessageEntity> Messages { get; set; } = new List<ConversationMessageEntity>();
}
