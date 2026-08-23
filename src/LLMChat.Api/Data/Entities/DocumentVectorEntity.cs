using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LLMChat.Api.Data.Entities;

public class DocumentVectorEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string DocumentId { get; set; } = string.Empty;

    public int ChunkId { get; set; }

    [Required]
    [MaxLength(512)]
    public string Source { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "TEXT")]
    public float[] Embedding { get; set; } = Array.Empty<float>();
}