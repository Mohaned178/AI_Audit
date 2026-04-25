using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class NotificationDeliveryOutcomeConfiguration : IEntityTypeConfiguration<NotificationDeliveryOutcome>
{
    public void Configure(EntityTypeBuilder<NotificationDeliveryOutcome> builder)
    {
        builder.ToTable("notification_delivery_outcomes");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.RecipientAddress)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(item => item.DeliveryStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.FinalReason)
            .HasMaxLength(2000);

        builder.HasIndex(item => new { item.NotificationId, item.RecipientUserId })
            .IsUnique();
        builder.HasIndex(item => new { item.DeliveryStatus, item.NextAttemptAtUtc });
        builder.HasIndex(item => new { item.NotificationId, item.DeliveryStatus });

        builder.HasOne<NotificationMessage>()
            .WithMany()
            .HasForeignKey(item => item.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
