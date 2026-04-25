using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class LimitEventConfiguration : IEntityTypeConfiguration<LimitEvent>
{
    public void Configure(EntityTypeBuilder<LimitEvent> builder)
    {
        builder.ToTable("limit_events");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Dimension)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.EventType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.TriggeredBySourceType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.TriggeredBySourceId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.CurrentQuantity)
            .HasPrecision(18, 4);

        builder.Property(item => item.ThresholdQuantity)
            .HasPrecision(18, 4);

        builder.Property(item => item.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasIndex(item => new { item.WorkspaceId, item.UsageCycleId, item.Dimension, item.OccurredAtUtc });
        builder.HasIndex(item => new { item.WorkspaceId, item.UsageCycleId, item.Dimension, item.EventType, item.TriggeredBySourceType, item.TriggeredBySourceId });

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(item => item.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<UsageCycle>()
            .WithMany()
            .HasForeignKey(item => item.UsageCycleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
