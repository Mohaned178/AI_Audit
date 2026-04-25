namespace AIUsageGuard.Application.RiskDetection.GetRiskPolicy;

public sealed record GetWorkspaceRiskPolicyQuery(
    Guid WorkspaceId,
    Guid RequestedByUserId);
