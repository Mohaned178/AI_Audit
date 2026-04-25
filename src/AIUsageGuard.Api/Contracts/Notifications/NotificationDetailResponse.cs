namespace AIUsageGuard.Api.Contracts.Notifications;

public sealed record NotificationDetailResponse(
    Guid Id,
    Guid WorkspaceId,
    string Type,
    string Channel,
    string? Severity,
    string Status,
    string Subject,
    string SummaryBody,
    string TriggerFingerprint,
    DateTimeOffset? CoveredPeriodStart,
    DateTimeOffset? CoveredPeriodEnd,
    DateTimeOffset CreatedAt,
    IReadOnlyList<NotificationDeliveryOutcomeResponse> Deliveries);
