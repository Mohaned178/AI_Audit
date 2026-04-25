namespace AIUsageGuard.Application.Models;

public sealed class NotificationMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; set; }

    public NotificationType NotificationType { get; set; }

    public string Channel { get; set; } = "email";

    public string TriggerFingerprint { get; set; } = string.Empty;

    public RiskSeverity? Severity { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string SummaryBody { get; set; } = string.Empty;

    public DateTimeOffset? CoveredPeriodStartUtc { get; set; }

    public DateTimeOffset? CoveredPeriodEndUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Guid CreatedByJobRunId { get; set; }

    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
}
