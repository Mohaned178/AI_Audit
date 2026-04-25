namespace AIUsageGuard.Application.Models;

public sealed class UsageCycle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; set; }

    public DateTimeOffset CycleStartUtc { get; set; }

    public DateTimeOffset CycleEndExclusiveUtc { get; set; }

    public Guid PlanAssignmentId { get; set; }

    public UsageCycleStatus Status { get; set; } = UsageCycleStatus.Open;

    public DateTimeOffset OpenedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ClosedAtUtc { get; set; }

    public DateTimeOffset LastCalculatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public int AdjustmentCount { get; set; }
}
