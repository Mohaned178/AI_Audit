using AIUsageGuard.Api.Contracts.Auth;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Billing;

internal static class BillingTestData
{
    internal sealed record BillingHistoryScenario(Guid CurrentCycleId, Guid PreviousCycleId);

    public static async Task SeedPlanStatusScenarioAsync(
        TestWebApplicationFactory factory,
        WorkspaceSessionResponse ownerSession)
    {
        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            await ClearWorkspaceBillingDataAsync(dbContext, ownerSession.WorkspaceId);
            var starterPlan = await EnsurePlanAsync(dbContext, "starter", "Starter", isDefault: true);
            var growthPlan = await EnsurePlanAsync(dbContext, "growth", "Growth", isDefault: false);
            var cycleStartUtc = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
            var cycleEndUtc = cycleStartUtc.AddMonths(1);

            var currentAssignment = new WorkspacePlanAssignment
            {
                WorkspaceId = ownerSession.WorkspaceId,
                PlanDefinitionId = starterPlan.Id,
                EffectiveFromCycleStartUtc = cycleStartUtc,
                AssignedByUserId = ownerSession.UserId,
                ChangeReason = "Seeded current plan"
            };
            var nextAssignment = new WorkspacePlanAssignment
            {
                WorkspaceId = ownerSession.WorkspaceId,
                PlanDefinitionId = growthPlan.Id,
                EffectiveFromCycleStartUtc = cycleEndUtc,
                AssignedByUserId = ownerSession.UserId,
                ChangeReason = "Seeded scheduled upgrade"
            };
            dbContext.WorkspacePlanAssignments.AddRange(currentAssignment, nextAssignment);

            var cycle = new UsageCycle
            {
                WorkspaceId = ownerSession.WorkspaceId,
                CycleStartUtc = cycleStartUtc,
                CycleEndExclusiveUtc = cycleEndUtc,
                PlanAssignmentId = currentAssignment.Id,
                Status = UsageCycleStatus.Open,
                OpenedAtUtc = cycleStartUtc,
                LastCalculatedAtUtc = cycleStartUtc.AddDays(21)
            };
            dbContext.UsageCycles.Add(cycle);
            dbContext.UsageCycleMetrics.AddRange(
                new UsageCycleMetric
                {
                    UsageCycleId = cycle.Id,
                    Dimension = BillingDimension.ActiveMembers,
                    IncludedQuantity = 10m,
                    WarningThresholdQuantity = 8m,
                    HardLimitQuantity = 10m,
                    CurrentQuantity = 8m,
                    LimitBehavior = BillingLimitBehavior.Restrict,
                    State = UsageCycleMetricState.Warning
                },
                new UsageCycleMetric
                {
                    UsageCycleId = cycle.Id,
                    Dimension = BillingDimension.AIActivityEvents,
                    IncludedQuantity = 5_000m,
                    WarningThresholdQuantity = 4_500m,
                    CurrentQuantity = 4_120m,
                    LimitBehavior = BillingLimitBehavior.AllowOverage,
                    State = UsageCycleMetricState.WithinLimit
                },
                new UsageCycleMetric
                {
                    UsageCycleId = cycle.Id,
                    Dimension = BillingDimension.EstimatedCost,
                    IncludedQuantity = 150m,
                    WarningThresholdQuantity = 120m,
                    CurrentQuantity = 132.45m,
                    LimitBehavior = BillingLimitBehavior.AllowOverage,
                    State = UsageCycleMetricState.Warning
                });

            await dbContext.SaveChangesAsync();
        });
    }

    public static async Task<BillingHistoryScenario> SeedBillingHistoryScenarioAsync(
        TestWebApplicationFactory factory,
        WorkspaceSessionResponse ownerSession)
    {
        return await factory.ExecuteDbContextAsync(async dbContext =>
        {
            await ClearWorkspaceBillingDataAsync(dbContext, ownerSession.WorkspaceId);
            var starterPlan = await EnsurePlanAsync(dbContext, "starter", "Starter", isDefault: true);
            var cycleStartUtc = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
            var previousCycleStartUtc = cycleStartUtc.AddMonths(-1);
            var currentAssignment = new WorkspacePlanAssignment
            {
                WorkspaceId = ownerSession.WorkspaceId,
                PlanDefinitionId = starterPlan.Id,
                EffectiveFromCycleStartUtc = previousCycleStartUtc,
                AssignedByUserId = ownerSession.UserId,
                ChangeReason = "Seeded current plan"
            };
            dbContext.WorkspacePlanAssignments.Add(currentAssignment);

            var currentCycle = new UsageCycle
            {
                WorkspaceId = ownerSession.WorkspaceId,
                CycleStartUtc = cycleStartUtc,
                CycleEndExclusiveUtc = cycleStartUtc.AddMonths(1),
                PlanAssignmentId = currentAssignment.Id,
                Status = UsageCycleStatus.Open,
                OpenedAtUtc = cycleStartUtc,
                LastCalculatedAtUtc = cycleStartUtc.AddDays(20)
            };
            var previousCycle = new UsageCycle
            {
                WorkspaceId = ownerSession.WorkspaceId,
                CycleStartUtc = previousCycleStartUtc,
                CycleEndExclusiveUtc = cycleStartUtc,
                PlanAssignmentId = currentAssignment.Id,
                Status = UsageCycleStatus.Adjusted,
                OpenedAtUtc = previousCycleStartUtc,
                ClosedAtUtc = cycleStartUtc,
                LastCalculatedAtUtc = cycleStartUtc.AddHours(1),
                AdjustmentCount = 1
            };

            dbContext.UsageCycles.AddRange(currentCycle, previousCycle);
            dbContext.UsageCycleMetrics.AddRange(
                CreateMetric(currentCycle.Id, BillingDimension.ActiveMembers, 10m, 8m, 10m, 8m, 0m, BillingLimitBehavior.Restrict, UsageCycleMetricState.Warning),
                CreateMetric(currentCycle.Id, BillingDimension.AIActivityEvents, 5_000m, 4_500m, null, 4_120m, 0m, BillingLimitBehavior.AllowOverage, UsageCycleMetricState.WithinLimit),
                CreateMetric(currentCycle.Id, BillingDimension.EstimatedCost, 150m, 120m, null, 132.45m, 0m, BillingLimitBehavior.AllowOverage, UsageCycleMetricState.Warning),
                CreateMetric(previousCycle.Id, BillingDimension.ActiveMembers, 10m, 8m, 10m, 7m, 0m, BillingLimitBehavior.Restrict, UsageCycleMetricState.WithinLimit),
                CreateMetric(previousCycle.Id, BillingDimension.AIActivityEvents, 5_000m, 4_500m, null, 5_235m, 235m, BillingLimitBehavior.AllowOverage, UsageCycleMetricState.Overage),
                CreateMetric(previousCycle.Id, BillingDimension.EstimatedCost, 150m, 120m, null, 161.20m, 11.20m, BillingLimitBehavior.AllowOverage, UsageCycleMetricState.Overage));

            dbContext.LimitEvents.AddRange(
                new LimitEvent
                {
                    WorkspaceId = ownerSession.WorkspaceId,
                    UsageCycleId = previousCycle.Id,
                    Dimension = BillingDimension.EstimatedCost,
                    EventType = LimitEventType.WarningRaised,
                    TriggeredBySourceType = "AIUsageEvent",
                    TriggeredBySourceId = Guid.NewGuid().ToString(),
                    CurrentQuantity = 123.40m,
                    ThresholdQuantity = 120m,
                    Reason = "Workspace estimated cost crossed the warning threshold for the Starter plan.",
                    OccurredAtUtc = new DateTimeOffset(2026, 3, 27, 9, 42, 0, TimeSpan.Zero)
                },
                new LimitEvent
                {
                    WorkspaceId = ownerSession.WorkspaceId,
                    UsageCycleId = previousCycle.Id,
                    Dimension = BillingDimension.AIActivityEvents,
                    EventType = LimitEventType.OverageStarted,
                    TriggeredBySourceType = "AIUsageEvent",
                    TriggeredBySourceId = Guid.NewGuid().ToString(),
                    CurrentQuantity = 5_001m,
                    ThresholdQuantity = 5_000m,
                    Reason = "Workspace activity volume moved into overage for the Starter plan.",
                    OccurredAtUtc = new DateTimeOffset(2026, 3, 31, 23, 10, 0, TimeSpan.Zero)
                });

            dbContext.CycleAdjustments.Add(new CycleAdjustment
            {
                UsageCycleId = previousCycle.Id,
                Dimension = BillingDimension.AIActivityEvents,
                DeltaQuantity = 234m,
                AdjustmentType = CycleAdjustmentType.LateActivity,
                SourceReference = "event-import-2026-04-01-0008",
                Reason = "Late accepted events for March were applied during reconciliation.",
                RecordedAtUtc = new DateTimeOffset(2026, 4, 1, 0, 8, 0, TimeSpan.Zero)
            });

            await dbContext.SaveChangesAsync();
            return new BillingHistoryScenario(currentCycle.Id, previousCycle.Id);
        });
    }

    public static async Task ConfigureLowAiActivityLimitAsync(
        TestWebApplicationFactory factory,
        Guid workspaceId,
        decimal includedQuantity,
        decimal warningThresholdQuantity,
        BillingLimitBehavior limitBehavior)
    {
        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            var cycle = await dbContext.UsageCycles.SingleAsync(item => item.WorkspaceId == workspaceId && item.Status == UsageCycleStatus.Open);
            var metric = await dbContext.UsageCycleMetrics.SingleAsync(item => item.UsageCycleId == cycle.Id && item.Dimension == BillingDimension.AIActivityEvents);
            metric.IncludedQuantity = includedQuantity;
            metric.WarningThresholdQuantity = warningThresholdQuantity;
            metric.HardLimitQuantity = limitBehavior == BillingLimitBehavior.Restrict ? includedQuantity : null;
            metric.LimitBehavior = limitBehavior;
            metric.CurrentQuantity = 0m;
            metric.OverageQuantity = 0m;
            metric.State = UsageCycleMetricState.WithinLimit;

            await dbContext.SaveChangesAsync();
        });
    }

    public static async Task SeedLateActivityScenarioAsync(
        TestWebApplicationFactory factory,
        WorkspaceSessionResponse ownerSession)
    {
        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            await ClearWorkspaceBillingDataAsync(dbContext, ownerSession.WorkspaceId);
            var starterPlan = await EnsurePlanAsync(dbContext, "starter", "Starter", isDefault: true);
            var previousCycleStartUtc = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
            var previousCycleEndUtc = previousCycleStartUtc.AddMonths(1);
            var assignment = new WorkspacePlanAssignment
            {
                WorkspaceId = ownerSession.WorkspaceId,
                PlanDefinitionId = starterPlan.Id,
                EffectiveFromCycleStartUtc = previousCycleStartUtc,
                AssignedByUserId = ownerSession.UserId,
                ChangeReason = "Seeded late-activity scenario"
            };
            dbContext.WorkspacePlanAssignments.Add(assignment);

            var previousCycle = new UsageCycle
            {
                WorkspaceId = ownerSession.WorkspaceId,
                CycleStartUtc = previousCycleStartUtc,
                CycleEndExclusiveUtc = previousCycleEndUtc,
                PlanAssignmentId = assignment.Id,
                Status = UsageCycleStatus.Closed,
                OpenedAtUtc = previousCycleStartUtc,
                ClosedAtUtc = previousCycleEndUtc,
                LastCalculatedAtUtc = previousCycleEndUtc
            };

            dbContext.UsageCycles.Add(previousCycle);
            dbContext.UsageCycleMetrics.AddRange(
                CreateMetric(previousCycle.Id, BillingDimension.ActiveMembers, 10m, 8m, 10m, 1m, 0m, BillingLimitBehavior.Restrict, UsageCycleMetricState.WithinLimit),
                CreateMetric(previousCycle.Id, BillingDimension.AIActivityEvents, 5_000m, 4_500m, null, 1m, 0m, BillingLimitBehavior.AllowOverage, UsageCycleMetricState.WithinLimit),
                CreateMetric(previousCycle.Id, BillingDimension.EstimatedCost, 150m, 120m, null, 2m, 0m, BillingLimitBehavior.AllowOverage, UsageCycleMetricState.WithinLimit));

            dbContext.AIUsageEvents.AddRange(
                CreateUsageEvent(ownerSession.WorkspaceId, ownerSession.UserId, previousCycleStartUtc.AddDays(5), "evt-1"),
                CreateUsageEvent(ownerSession.WorkspaceId, ownerSession.UserId, previousCycleStartUtc.AddDays(6), "evt-2"));

            await dbContext.SaveChangesAsync();
        });
    }

    private static async Task<PlanDefinition> EnsurePlanAsync(
        ApplicationDbContext dbContext,
        string planCode,
        string displayName,
        bool isDefault)
    {
        var existing = await dbContext.PlanDefinitions.FirstOrDefaultAsync(item => item.PlanCode == planCode);
        if (existing is not null)
        {
            return existing;
        }

        var plan = new PlanDefinition
        {
            PlanCode = planCode,
            DisplayName = displayName,
            IsDefault = isDefault,
            IsActive = true
        };
        dbContext.PlanDefinitions.Add(plan);
        await dbContext.SaveChangesAsync();
        return plan;
    }

    private static async Task ClearWorkspaceBillingDataAsync(ApplicationDbContext dbContext, Guid workspaceId)
    {
        var cycleIds = await dbContext.UsageCycles
            .Where(item => item.WorkspaceId == workspaceId)
            .Select(item => item.Id)
            .ToListAsync();

        if (cycleIds.Count > 0)
        {
            dbContext.UsageCycleMetrics.RemoveRange(dbContext.UsageCycleMetrics.Where(item => cycleIds.Contains(item.UsageCycleId)));
            dbContext.CycleAdjustments.RemoveRange(dbContext.CycleAdjustments.Where(item => cycleIds.Contains(item.UsageCycleId)));
            dbContext.LimitEvents.RemoveRange(dbContext.LimitEvents.Where(item => cycleIds.Contains(item.UsageCycleId)));
            dbContext.UsageCycles.RemoveRange(dbContext.UsageCycles.Where(item => item.WorkspaceId == workspaceId));
        }

        dbContext.WorkspacePlanAssignments.RemoveRange(dbContext.WorkspacePlanAssignments.Where(item => item.WorkspaceId == workspaceId));
        await dbContext.SaveChangesAsync();
    }

    private static UsageCycleMetric CreateMetric(
        Guid cycleId,
        BillingDimension dimension,
        decimal includedQuantity,
        decimal? warningThresholdQuantity,
        decimal? hardLimitQuantity,
        decimal currentQuantity,
        decimal overageQuantity,
        BillingLimitBehavior limitBehavior,
        UsageCycleMetricState state)
    {
        return new UsageCycleMetric
        {
            UsageCycleId = cycleId,
            Dimension = dimension,
            IncludedQuantity = includedQuantity,
            WarningThresholdQuantity = warningThresholdQuantity,
            HardLimitQuantity = hardLimitQuantity,
            CurrentQuantity = currentQuantity,
            OverageQuantity = overageQuantity,
            LimitBehavior = limitBehavior,
            State = state
        };
    }

    private static AIUsageEvent CreateUsageEvent(
        Guid workspaceId,
        Guid actorUserId,
        DateTimeOffset occurredAtUtc,
        string idempotencyKey)
    {
        return new AIUsageEvent
        {
            WorkspaceId = workspaceId,
            ActorUserId = actorUserId,
            EventType = AIUsageEventType.ToolUsed,
            IdempotencyKey = idempotencyKey,
            ToolName = "copilot",
            OccurredAt = occurredAtUtc,
            ReceivedAt = occurredAtUtc.AddMinutes(1),
            EstimatedCost = 1m
        };
    }
}
