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

    public DbSet<ConversationSessionEntity> ConversationSessions { get; set; }

    public DbSet<ConversationMessageEntity> ConversationMessages { get; set; }

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

        modelBuilder.Entity<ConversationSessionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId)
                .IsRequired()
                .HasMaxLength(256);
            entity.HasIndex(e => e.SessionId)
                .IsUnique()
                .HasDatabaseName("IX_ConversationSession_SessionId");
            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();
            entity.Property(e => e.UpdatedAtUtc)
                .IsRequired();
        });

        modelBuilder.Entity<ConversationMessageEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(32);
            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnType("TEXT");
            entity.Property(e => e.ToolCallsJson)
                .HasColumnType("TEXT");
            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();

            entity.HasOne(e => e.ConversationSession)
                .WithMany(s => s.Messages)
                .HasForeignKey(e => e.ConversationSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ConversationSessionId, e.SequenceNumber })
                .HasDatabaseName("IX_ConversationMessage_SessionId_SequenceNumber");
        });
    }
}
