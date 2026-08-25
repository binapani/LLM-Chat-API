using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LLMChat.Api.Data.Entities;

[Table("Documents")]
public class DocumentEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string ContentType { get; set; } = string.Empty;

    public DateTime UploadedAtUtc { get; set; }

    [Required]
    [MaxLength(512)]
    public string Source { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "TEXT")]
    public string Content { get; set; } = string.Empty;
}
