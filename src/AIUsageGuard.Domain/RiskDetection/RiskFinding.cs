namespace AIUsageGuard.Domain.RiskDetection;

public sealed class RiskFinding
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; set; }

    public Guid EventId { get; set; }

    public Guid EvaluationOutcomeId { get; set; }

    public RiskRuleType RuleType { get; set; }

    public RiskSeverity Severity { get; set; }

    public RiskFindingStatus Status { get; set; } = RiskFindingStatus.Open;

    public string Reason { get; set; } = string.Empty;

    public string? EvidencePreview { get; set; }

    public Guid ActorUserId { get; set; }

    public string ToolName { get; set; } = string.Empty;

    public DateTimeOffset DetectedAt { get; set; } = DateTimeOffset.UtcNow;
}
