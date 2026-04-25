using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class UsageCycleMetricConfiguration : IEntityTypeConfiguration<UsageCycleMetric>
{
    public void Configure(EntityTypeBuilder<UsageCycleMetric> builder)
    {
        builder.ToTable("usage_cycle_metrics");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Dimension)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.LimitBehavior)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.State)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(item => item.IncludedQuantity)
            .HasPrecision(18, 4);

        builder.Property(item => item.WarningThresholdQuantity)
            .HasPrecision(18, 4);

        builder.Property(item => item.HardLimitQuantity)
            .HasPrecision(18, 4);

        builder.Property(item => item.CurrentQuantity)
            .HasPrecision(18, 4);

        builder.Property(item => item.OverageQuantity)
            .HasPrecision(18, 4);

        builder.HasIndex(item => new { item.UsageCycleId, item.Dimension })
            .IsUnique();

        builder.HasOne<UsageCycle>()
            .WithMany()
            .HasForeignKey(item => item.UsageCycleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
