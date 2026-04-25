namespace AIUsageGuard.Domain.RiskDetection;

public sealed class RiskEvaluationOutcome
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; set; }

    public Guid EventId { get; set; }

    public DateTimeOffset EvaluatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string AppliedRuleVersion { get; set; } = string.Empty;

    public int MatchedRuleCount { get; set; }

    public RiskEvaluationResult EvaluationResult { get; set; }

    public string? SkippedReason { get; set; }

    public string? EvidenceSummary { get; set; }
}
