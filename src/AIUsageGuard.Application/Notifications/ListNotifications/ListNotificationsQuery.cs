using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Notifications.ListNotifications;

public sealed record ListNotificationsQuery(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    NotificationType? NotificationType,
    NotificationStatus? Status,
    DateTimeOffset? FromCreatedAt,
    DateTimeOffset? ToCreatedAt,
    int PageNumber,
    int PageSize);
