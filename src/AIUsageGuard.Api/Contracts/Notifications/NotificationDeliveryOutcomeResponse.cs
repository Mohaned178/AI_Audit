namespace AIUsageGuard.Api.Contracts.Notifications;

public sealed record NotificationDeliveryOutcomeResponse(
    Guid RecipientUserId,
    string RecipientAddress,
    string Status,
    int AttemptCount,
    DateTimeOffset? LastAttemptedAt,
    DateTimeOffset? NextAttemptAt,
    string? FinalReason);
