namespace AIUsageGuard.Application.Models;

public sealed class UsageCycleMetric
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UsageCycleId { get; set; }

    public BillingDimension Dimension { get; set; }

    public decimal IncludedQuantity { get; set; }

    public decimal? WarningThresholdQuantity { get; set; }

    public decimal? HardLimitQuantity { get; set; }

    public decimal CurrentQuantity { get; set; }

    public decimal OverageQuantity { get; set; }

    public BillingLimitBehavior LimitBehavior { get; set; }

    public UsageCycleMetricState State { get; set; } = UsageCycleMetricState.WithinLimit;

    public DateTimeOffset? LastTransitionAtUtc { get; set; }
}
