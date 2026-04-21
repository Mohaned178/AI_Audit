using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class AuditRecordConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> builder)
    {
        builder.ToTable("audit_records");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.ActionType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(record => record.TargetType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(record => record.Result)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(record => record.Reason)
            .HasMaxLength(512)
            .IsRequired();

        builder.HasIndex(record => record.WorkspaceId);
        builder.HasIndex(record => record.ActorUserId);
        builder.HasIndex(record => record.OccurredAt);
    }
}
