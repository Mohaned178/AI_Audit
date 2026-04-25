namespace AIUsageGuard.Application.RiskDetection.GetFinding;

public sealed record GetRiskFindingQuery(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    Guid FindingId);
