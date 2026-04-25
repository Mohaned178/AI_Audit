using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class PlanDefinitionConfiguration : IEntityTypeConfiguration<PlanDefinition>
{
    public void Configure(EntityTypeBuilder<PlanDefinition> builder)
    {
        builder.ToTable("plan_definitions");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.PlanCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(item => item.PlanCode)
            .IsUnique();

        builder.HasIndex(item => new { item.IsActive, item.IsDefault });
    }
}
