using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class RiskFindingConfiguration : IEntityTypeConfiguration<RiskFinding>
{
    public void Configure(EntityTypeBuilder<RiskFinding> builder)
    {
        builder.ToTable("risk_findings");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.RuleType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.Severity)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(item => item.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(item => item.EvidencePreview)
            .HasMaxLength(2000);

        builder.Property(item => item.ToolName)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(item => new { item.WorkspaceId, item.EventId, item.RuleType })
            .IsUnique();

        builder.HasIndex(item => new { item.WorkspaceId, item.DetectedAt });
        builder.HasIndex(item => new { item.WorkspaceId, item.Severity, item.DetectedAt });
        builder.HasIndex(item => new { item.WorkspaceId, item.RuleType, item.DetectedAt });
        builder.HasIndex(item => new { item.WorkspaceId, item.ActorUserId, item.DetectedAt });
        builder.HasIndex(item => new { item.WorkspaceId, item.ToolName, item.DetectedAt });

        builder.HasOne<AIUsageEvent>()
            .WithMany()
            .HasForeignKey(item => item.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RiskEvaluationOutcome>()
            .WithMany()
            .HasForeignKey(item => item.EvaluationOutcomeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(item => item.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
