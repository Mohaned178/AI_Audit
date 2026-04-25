namespace AIUsageGuard.Api.Contracts.Billing;

public sealed record PlanMetricStatusResponse(
    string Dimension,
    decimal IncludedQuantity,
    decimal? WarningThresholdQuantity,
    decimal? HardLimitQuantity,
    decimal CurrentQuantity,
    decimal RemainingQuantity,
    decimal OverageQuantity,
    string LimitBehavior,
    string State);
