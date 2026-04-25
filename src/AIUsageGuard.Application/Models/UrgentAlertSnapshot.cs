namespace AIUsageGuard.Application.Models;

public sealed class UrgentAlertSnapshot
{
    public Guid WorkspaceId { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public string SourceId { get; set; } = string.Empty;

    public RiskSeverity Severity { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? ActorDisplayLabel { get; set; }

    public string? ToolLabel { get; set; }

    public DateTimeOffset ObservedAtUtc { get; set; }
}
