using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Notifications.ListNotifications;

public sealed record NotificationListItem(
    NotificationMessage Notification,
    int RecipientCount,
    int DeliveredCount,
    int FailedCount);
