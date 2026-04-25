namespace AIUsageGuard.Application.RiskDetection.UpdateRiskPolicy;

public sealed record UpdateWorkspaceRiskPolicyCommand(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    IReadOnlyList<string>? ApprovedTools,
    decimal? PerEventEstimatedCostThreshold,
    decimal? DailyEstimatedCostThreshold);
