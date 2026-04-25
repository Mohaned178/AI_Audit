namespace AIUsageGuard.Api.Contracts.RiskDetection;

public sealed record RiskFindingListItemResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid EventId,
    Guid EvaluationOutcomeId,
    string RuleType,
    string Severity,
    string Status,
    string Reason,
    string? EvidencePreview,
    Guid ActorUserId,
    string ToolName,
    DateTimeOffset DetectedAt);
