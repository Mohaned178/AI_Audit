namespace AIUsageGuard.Application.Notifications.ListNotifications;

public sealed record ListNotificationsResult(
    IReadOnlyList<NotificationListItem> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
