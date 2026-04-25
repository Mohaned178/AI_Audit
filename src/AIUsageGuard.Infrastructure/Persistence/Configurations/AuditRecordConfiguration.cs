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

        builder.Property(record => record.Category)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(record => record.CorrelationId)
            .HasMaxLength(128);

        builder.Property(record => record.ClientIpAddressHash)
            .HasMaxLength(128);

        builder.Property(record => record.UserAgent)
            .HasMaxLength(512);

        builder.Property(record => record.IsSecurityRelevant)
            .IsRequired();

        builder.HasIndex(record => new { record.WorkspaceId, record.OccurredAt });
        builder.HasIndex(record => new { record.WorkspaceId, record.ActionType, record.OccurredAt });
        builder.HasIndex(record => new { record.WorkspaceId, record.Result, record.OccurredAt });
        builder.HasIndex(record => new { record.WorkspaceId, record.ActorUserId, record.OccurredAt });
        builder.HasIndex(record => new { record.WorkspaceId, record.IsSecurityRelevant, record.OccurredAt });
        builder.HasIndex(record => record.ActorUserId);
    }
}
