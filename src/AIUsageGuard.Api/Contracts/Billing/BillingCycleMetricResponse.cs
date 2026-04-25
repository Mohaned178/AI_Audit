namespace AIUsageGuard.Api.Contracts.Billing;

public sealed record BillingCycleMetricResponse(
    string Dimension,
    decimal IncludedQuantity,
    decimal CurrentQuantity,
    decimal OverageQuantity,
    string State,
    string LimitBehavior);
