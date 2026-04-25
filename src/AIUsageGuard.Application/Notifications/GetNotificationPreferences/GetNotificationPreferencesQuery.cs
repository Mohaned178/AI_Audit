namespace AIUsageGuard.Application.Notifications.GetNotificationPreferences;

public sealed record GetNotificationPreferencesQuery(Guid WorkspaceId, Guid RequestedByUserId);
