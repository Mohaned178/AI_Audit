namespace AIUsageGuard.Application.Notifications.GetNotification;

public sealed record GetNotificationQuery(Guid WorkspaceId, Guid RequestedByUserId, Guid NotificationId);
