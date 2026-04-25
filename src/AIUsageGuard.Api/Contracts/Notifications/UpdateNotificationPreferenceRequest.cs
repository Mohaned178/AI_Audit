namespace AIUsageGuard.Api.Contracts.Notifications;

public sealed record UpdateNotificationPreferenceRequest(
    bool UrgentAlertsEnabled,
    bool DigestEnabled,
    string? DigestCadence,
    string RecipientSelectionMode,
    IReadOnlyList<Guid>? SelectedRecipientUserIds);
