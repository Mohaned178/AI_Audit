namespace AIUsageGuard.Application.Notifications.DeliverPendingNotifications;

public sealed record DeliverPendingNotificationsResult(
    int ProcessedNotificationCount,
    int DeliveredCount,
    int RetryScheduledCount,
    int FailedCount,
    int SkippedCount);
