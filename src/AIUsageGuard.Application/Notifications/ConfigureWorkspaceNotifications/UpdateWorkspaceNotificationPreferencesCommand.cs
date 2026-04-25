using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Notifications.ConfigureWorkspaceNotifications;

public sealed record UpdateWorkspaceNotificationPreferencesCommand(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    bool UrgentAlertsEnabled,
    bool DigestEnabled,
    DigestCadence? DigestCadence,
    RecipientSelectionMode RecipientSelectionMode,
    IReadOnlyList<Guid> SelectedRecipientUserIds);
