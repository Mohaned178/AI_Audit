using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspaces");
        builder.HasKey(workspace => workspace.Id);

        builder.Property(workspace => workspace.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(workspace => workspace.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(workspace => workspace.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(workspace => workspace.Slug)
            .IsUnique();
    }
}
