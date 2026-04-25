using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class UsageCycleConfiguration : IEntityTypeConfiguration<UsageCycle>
{
    public void Configure(EntityTypeBuilder<UsageCycle> builder)
    {
        builder.ToTable("usage_cycles");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(item => new { item.WorkspaceId, item.CycleStartUtc })
            .IsUnique();

        builder.HasIndex(item => new { item.WorkspaceId, item.Status, item.CycleStartUtc });

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(item => item.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<WorkspacePlanAssignment>()
            .WithMany()
            .HasForeignKey(item => item.PlanAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
