namespace AIUsageGuard.Api.Contracts.RiskDetection;

public sealed record UpdateWorkspaceRiskPolicyRequest(
    IReadOnlyList<string>? ApprovedTools,
    decimal? PerEventEstimatedCostThreshold,
    decimal? DailyEstimatedCostThreshold);
