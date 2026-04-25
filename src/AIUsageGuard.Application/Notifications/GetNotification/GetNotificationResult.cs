using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Notifications.GetNotification;

public sealed record GetNotificationResult(
    NotificationMessage Notification,
    IReadOnlyList<NotificationDeliveryOutcome> DeliveryOutcomes);
