namespace AIUsageGuard.Application.Models;

public sealed class NotificationDeliveryOutcome
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid NotificationId { get; set; }

    public Guid RecipientUserId { get; set; }

    public string RecipientAddress { get; set; } = string.Empty;

    public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.Pending;

    public int AttemptCount { get; set; }

    public DateTimeOffset? LastAttemptedAtUtc { get; set; }

    public DateTimeOffset? NextAttemptAtUtc { get; set; }

    public string? FinalReason { get; set; }
}
