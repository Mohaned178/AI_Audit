namespace AIUsageGuard.Application.Models;

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

    public string Category { get; set; } = "governance";

    public bool IsSecurityRelevant { get; set; }

    public string? CorrelationId { get; set; }

    public string? ClientIpAddressHash { get; set; }

    public string? UserAgent { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
