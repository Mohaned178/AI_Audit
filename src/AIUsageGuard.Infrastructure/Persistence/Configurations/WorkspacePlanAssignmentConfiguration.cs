using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class WorkspacePlanAssignmentConfiguration : IEntityTypeConfiguration<WorkspacePlanAssignment>
{
    public void Configure(EntityTypeBuilder<WorkspacePlanAssignment> builder)
    {
        builder.ToTable("workspace_plan_assignments");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.ChangeReason)
            .HasMaxLength(512);

        builder.HasIndex(item => new { item.WorkspaceId, item.EffectiveFromCycleStartUtc });
        builder.HasIndex(item => new { item.WorkspaceId, item.EffectiveToCycleStartUtc });

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(item => item.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<PlanDefinition>()
            .WithMany()
            .HasForeignKey(item => item.PlanDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
