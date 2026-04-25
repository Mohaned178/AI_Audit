using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.NotificationType)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(item => item.Channel)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.TriggerFingerprint)
            .HasMaxLength(400)
            .IsRequired();

        builder.Property(item => item.Severity)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(item => item.Subject)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(item => item.SummaryBody)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(item => new { item.WorkspaceId, item.NotificationType, item.TriggerFingerprint })
            .IsUnique();
        builder.HasIndex(item => new { item.WorkspaceId, item.CreatedAtUtc });
        builder.HasIndex(item => new { item.WorkspaceId, item.NotificationType, item.CreatedAtUtc });
        builder.HasIndex(item => new { item.WorkspaceId, item.Status, item.CreatedAtUtc });
    }
}
