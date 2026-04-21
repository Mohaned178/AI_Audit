using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceMembershipConfiguration : IEntityTypeConfiguration<WorkspaceMembership>
{
    public void Configure(EntityTypeBuilder<WorkspaceMembership> builder)
    {
        builder.ToTable("workspace_memberships");
        builder.HasKey(membership => membership.Id);

        builder.Property(membership => membership.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(membership => membership.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(membership => new { membership.WorkspaceId, membership.UserId });

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(membership => membership.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
