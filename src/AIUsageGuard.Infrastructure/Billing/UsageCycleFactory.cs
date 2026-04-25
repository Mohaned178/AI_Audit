using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Infrastructure.Billing;

public sealed class UsageCycleFactory
{
    public (UsageCycle Cycle, IReadOnlyList<UsageCycleMetric> Metrics) CreateMonthlyCycle(
        Guid workspaceId,
        WorkspacePlanAssignment assignment,
        DateTimeOffset cycleStartUtc,
        IReadOnlyList<PlanLimitRule> rules,
        DateTimeOffset openedAtUtc)
    {
        var cycle = new UsageCycle
        {
            WorkspaceId = workspaceId,
            CycleStartUtc = cycleStartUtc,
            CycleEndExclusiveUtc = cycleStartUtc.AddMonths(1),
            PlanAssignmentId = assignment.Id,
            Status = UsageCycleStatus.Open,
            OpenedAtUtc = openedAtUtc,
            LastCalculatedAtUtc = openedAtUtc
        };

        var metrics = rules
            .Select(rule => new UsageCycleMetric
            {
                UsageCycleId = cycle.Id,
                Dimension = rule.Dimension,
                IncludedQuantity = rule.IncludedQuantity,
                WarningThresholdQuantity = rule.WarningThresholdQuantity,
                HardLimitQuantity = rule.HardLimitQuantity,
                CurrentQuantity = 0m,
                OverageQuantity = 0m,
                LimitBehavior = rule.LimitBehavior,
                State = UsageCycleMetricState.WithinLimit
            })
            .ToList();

        return (cycle, metrics);
    }
}
