namespace AIUsageGuard.Domain.Auditing;

public sealed class AuditRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? WorkspaceId { get; set; }

    public Guid? ActorUserId { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string? TargetId { get; set; }

    public string Result { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
