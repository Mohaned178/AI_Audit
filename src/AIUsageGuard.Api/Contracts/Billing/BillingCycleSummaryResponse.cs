namespace AIUsageGuard.Api.Contracts.Billing;

public sealed record BillingCycleSummaryResponse(
    Guid CycleId,
    DateTimeOffset CycleStartUtc,
    DateTimeOffset CycleEndExclusiveUtc,
    string Status,
    string PlanCode,
    string PlanDisplayName,
    IReadOnlyList<BillingCycleMetricResponse> Metrics,
    int WarningEventCount,
    int RestrictionEventCount,
    int AdjustmentCount);
