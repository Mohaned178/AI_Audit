using System.Text.Json;
using AIUsageGuard.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AIUsageGuard.Infrastructure.Persistence.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        var listConverter = new ValueConverter<List<Guid>, string>(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<List<Guid>>(value, (JsonSerializerOptions?)null) ?? new List<Guid>());
        var listComparer = new ValueComparer<List<Guid>>(
            (left, right) => (left ?? new List<Guid>()).SequenceEqual(right ?? new List<Guid>()),
            value => value.Aggregate(0, (current, item) => HashCode.Combine(current, item.GetHashCode())),
            value => value.ToList());

        builder.ToTable("notification_preferences");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.DigestCadence)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(item => item.RecipientSelectionMode)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.SelectedRecipientUserIds)
            .HasConversion(listConverter)
            .Metadata.SetValueComparer(listComparer);

        builder.Property(item => item.SelectedRecipientUserIds)
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(item => item.WorkspaceId)
            .IsUnique();
    }
}
