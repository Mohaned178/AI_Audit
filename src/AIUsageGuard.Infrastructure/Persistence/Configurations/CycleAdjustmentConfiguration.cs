using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class CycleAdjustmentConfiguration : IEntityTypeConfiguration<CycleAdjustment>
{
    public void Configure(EntityTypeBuilder<CycleAdjustment> builder)
    {
        builder.ToTable("cycle_adjustments");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Dimension)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.AdjustmentType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.DeltaQuantity)
            .HasPrecision(18, 4);

        builder.Property(item => item.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(item => item.SourceReference)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(item => new { item.UsageCycleId, item.RecordedAtUtc });

        builder.HasOne<UsageCycle>()
            .WithMany()
            .HasForeignKey(item => item.UsageCycleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<BackgroundJobRun>()
            .WithMany()
            .HasForeignKey(item => item.AppliedByJobRunId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
