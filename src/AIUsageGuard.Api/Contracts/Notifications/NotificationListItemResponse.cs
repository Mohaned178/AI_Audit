namespace AIUsageGuard.Api.Contracts.Notifications;

public sealed record NotificationListItemResponse(
    Guid Id,
    Guid WorkspaceId,
    string Type,
    string Channel,
    string? Severity,
    string Status,
    string Subject,
    DateTimeOffset? CoveredPeriodStart,
    DateTimeOffset? CoveredPeriodEnd,
    DateTimeOffset CreatedAt,
    int RecipientCount,
    int DeliveredCount,
    int FailedCount);
