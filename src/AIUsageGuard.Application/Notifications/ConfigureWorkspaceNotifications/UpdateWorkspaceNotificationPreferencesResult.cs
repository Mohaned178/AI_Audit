using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Notifications.ConfigureWorkspaceNotifications;

public sealed record UpdateWorkspaceNotificationPreferencesResult(
    NotificationPreference Preference,
    bool CreatedNewPreference);
