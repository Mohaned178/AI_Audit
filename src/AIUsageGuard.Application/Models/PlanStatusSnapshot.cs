namespace AIUsageGuard.Application.Models;

public sealed class PlanStatusSnapshot
{
    public Guid WorkspaceId { get; set; }

    public string PlanCode { get; set; } = string.Empty;

    public string PlanDisplayName { get; set; } = string.Empty;

    public Guid CurrentCycleId { get; set; }

    public DateTimeOffset CycleStartUtc { get; set; }

    public DateTimeOffset CycleEndExclusiveUtc { get; set; }

    public string? NextPlanCode { get; set; }

    public DateTimeOffset? NextPlanStartsAtUtc { get; set; }

    public IReadOnlyList<UsageCycleMetric> MetricStatuses { get; set; } = [];
}
