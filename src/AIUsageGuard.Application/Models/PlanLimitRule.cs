namespace AIUsageGuard.Application.Models;

public sealed class PlanLimitRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PlanDefinitionId { get; set; }

    public BillingDimension Dimension { get; set; }

    public decimal IncludedQuantity { get; set; }

    public decimal? WarningThresholdQuantity { get; set; }

    public decimal? HardLimitQuantity { get; set; }

    public BillingLimitBehavior LimitBehavior { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
