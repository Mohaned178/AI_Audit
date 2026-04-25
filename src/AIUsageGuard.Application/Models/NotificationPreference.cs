namespace AIUsageGuard.Application.Models;

public sealed class NotificationPreference
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; set; }

    public bool UrgentAlertsEnabled { get; set; }

    public bool DigestEnabled { get; set; }

    public DigestCadence? DigestCadence { get; set; }

    public RecipientSelectionMode RecipientSelectionMode { get; set; } = RecipientSelectionMode.AllAdminsAndOwners;

    public List<Guid> SelectedRecipientUserIds { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid LastUpdatedByUserId { get; set; }
}
