using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class PlanLimitRuleConfiguration : IEntityTypeConfiguration<PlanLimitRule>
{
    public void Configure(EntityTypeBuilder<PlanLimitRule> builder)
    {
        builder.ToTable("plan_limit_rules");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Dimension)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.LimitBehavior)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.IncludedQuantity)
            .HasPrecision(18, 4);

        builder.Property(item => item.WarningThresholdQuantity)
            .HasPrecision(18, 4);

        builder.Property(item => item.HardLimitQuantity)
            .HasPrecision(18, 4);

        builder.HasIndex(item => new { item.PlanDefinitionId, item.Dimension })
            .IsUnique();

        builder.HasOne<PlanDefinition>()
            .WithMany()
            .HasForeignKey(item => item.PlanDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
