namespace AIUsageGuard.Api.Contracts.RiskDetection;

public sealed record WorkspaceRiskPolicyResponse(
    Guid WorkspaceId,
    IReadOnlyList<string> ApprovedTools,
    decimal? PerEventEstimatedCostThreshold,
    decimal? DailyEstimatedCostThreshold,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt,
    Guid LastUpdatedByUserId);
