using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class RiskEvaluationOutcomeConfiguration : IEntityTypeConfiguration<RiskEvaluationOutcome>
{
    public void Configure(EntityTypeBuilder<RiskEvaluationOutcome> builder)
    {
        builder.ToTable("risk_evaluation_outcomes");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.AppliedRuleVersion)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.EvaluationResult)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(item => item.SkippedReason)
            .HasMaxLength(1000);

        builder.Property(item => item.EvidenceSummary)
            .HasMaxLength(2000);

        builder.HasIndex(item => item.EventId)
            .IsUnique();
        builder.HasIndex(item => new { item.WorkspaceId, item.EvaluatedAt });

        builder.HasOne<AIUsageEvent>()
            .WithMany()
            .HasForeignKey(item => item.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(item => item.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
