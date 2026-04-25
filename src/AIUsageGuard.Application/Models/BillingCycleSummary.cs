namespace AIUsageGuard.Application.Models;

public sealed class BillingCycleSummary
{
    public Guid UsageCycleId { get; set; }

    public Guid WorkspaceId { get; set; }

    public string PlanCode { get; set; } = string.Empty;

    public string PlanDisplayName { get; set; } = string.Empty;

    public DateTimeOffset CycleStartUtc { get; set; }

    public DateTimeOffset CycleEndExclusiveUtc { get; set; }

    public UsageCycleStatus Status { get; set; }

    public IReadOnlyList<UsageCycleMetric> Metrics { get; set; } = [];

    public int WarningEventCount { get; set; }

    public int RestrictionEventCount { get; set; }

    public int AdjustmentCount { get; set; }
}
