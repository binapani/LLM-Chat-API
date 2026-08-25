using System.Text.Json;
using LLMChat.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LLMChat.Api.Data;

public class VectorDbContext : DbContext
{
    public VectorDbContext(DbContextOptions<VectorDbContext> options)
        : base(options)
    {
    }

    public DbSet<DocumentEntity> Documents { get; set; }

    public DbSet<DocumentVectorEntity> DocumentVectors => Set<DocumentVectorEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

       
        var embeddingConverter = new ValueConverter<float[], string>(
    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
    v => JsonSerializer.Deserialize<float[]>(v, (JsonSerializerOptions?)null)
         ?? Array.Empty<float>());

        modelBuilder.Entity<DocumentVectorEntity>()
            .Property(e => e.Embedding)
            .HasConversion(embeddingConverter)
            .HasColumnType("TEXT");

        modelBuilder.Entity<DocumentVectorEntity>()
            .HasIndex(e => e.DocumentId)
            .HasDatabaseName("IX_DocumentVectorEntity_DocumentId");

        modelBuilder.Entity<DocumentVectorEntity>()
            .HasIndex(e => new { e.DocumentId, e.ChunkId })
            .HasDatabaseName("IX_DocumentVectorEntity_DocumentId_ChunkId");
    }
}
