using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class AIUsageEventConfiguration : IEntityTypeConfiguration<AIUsageEvent>
{
    public void Configure(EntityTypeBuilder<AIUsageEvent> builder)
    {
        builder.ToTable("ai_usage_events");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.EventType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.IdempotencyKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.ToolName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.ModelName)
            .HasMaxLength(200);

        builder.Property(item => item.SourceLabel)
            .HasMaxLength(200);

        builder.Property(item => item.PromptPreview)
            .HasMaxLength(4000);

        builder.Property(item => item.FileName)
            .HasMaxLength(512);

        builder.Property(item => item.DetailsJson)
            .HasColumnType("text");

        builder.Property(item => item.EstimatedCost)
            .HasPrecision(18, 4);

        builder.HasIndex(item => new { item.WorkspaceId, item.IdempotencyKey })
            .IsUnique();

        builder.HasIndex(item => new { item.WorkspaceId, item.OccurredAt });
        builder.HasIndex(item => new { item.WorkspaceId, item.EventType, item.OccurredAt });
        builder.HasIndex(item => new { item.WorkspaceId, item.ActorUserId, item.OccurredAt });
        builder.HasIndex(item => new { item.WorkspaceId, item.ToolName, item.OccurredAt });
        builder.HasIndex(item => new { item.WorkspaceId, item.OccurredAt, item.EstimatedCost });

    }
}
