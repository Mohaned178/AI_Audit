namespace AIUsageGuard.Api.Contracts.Notifications;

public sealed record NotificationPreferenceResponse(
    Guid WorkspaceId,
    bool UrgentAlertsEnabled,
    bool DigestEnabled,
    string? DigestCadence,
    string RecipientSelectionMode,
    IReadOnlyList<Guid> SelectedRecipientUserIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt,
    Guid LastUpdatedByUserId);
