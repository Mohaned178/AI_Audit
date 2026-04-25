using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Persistence;

namespace AIUsageGuard.UnitTests.Billing;

internal static class BillingUnitTestData
{
    public static async Task<(Workspace Workspace, UserAccount Owner, Guid PreviousCycleId)> SeedBillingHistoryAsync(ApplicationDbContext store)
    {
        var owner = new UserAccount
        {
            Email = "owner@example.com",
            DisplayName = "Owner",
            PasswordHash = "hash"
        };
        var workspace = new Workspace
        {
            Name = "Alpha Workspace",
            Slug = "alpha-workspace",
            CreatedByUserId = owner.Id
        };
        var membership = new WorkspaceMembership
        {
            WorkspaceId = workspace.Id,
            UserId = owner.Id,
            Role = WorkspaceRole.Owner,
            Status = MembershipStatus.Active
        };

        await store.AddUserAsync(owner);
        await store.AddWorkspaceAsync(workspace);
        await store.AddMembershipAsync(membership);

        var plan = new PlanDefinition
        {
            PlanCode = "starter",
            DisplayName = "Starter",
            IsDefault = true,
            IsActive = true
        };
        await store.AddPlanDefinitionAsync(plan);

        var assignment = new WorkspacePlanAssignment
        {
            WorkspaceId = workspace.Id,
            PlanDefinitionId = plan.Id,
            EffectiveFromCycleStartUtc = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            AssignedByUserId = owner.Id,
            ChangeReason = "Seed"
        };
        await store.AddPlanAssignmentAsync(assignment);

        var currentCycle = new UsageCycle
        {
            WorkspaceId = workspace.Id,
            CycleStartUtc = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            CycleEndExclusiveUtc = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            PlanAssignmentId = assignment.Id,
            Status = UsageCycleStatus.Open
        };
        var previousCycle = new UsageCycle
        {
            WorkspaceId = workspace.Id,
            CycleStartUtc = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            CycleEndExclusiveUtc = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            PlanAssignmentId = assignment.Id,
            Status = UsageCycleStatus.Adjusted,
            AdjustmentCount = 1
        };
        await store.AddUsageCycleAsync(currentCycle);
        await store.AddUsageCycleAsync(previousCycle);

        foreach (var metric in CreateMetrics(currentCycle.Id, 8m, 4_120m, 132.45m, 0m, 0m))
        {
            await store.AddUsageCycleMetricAsync(metric);
        }

        foreach (var metric in CreateMetrics(previousCycle.Id, 7m, 5_235m, 161.20m, 235m, 11.20m))
        {
            await store.AddUsageCycleMetricAsync(metric);
        }

        await store.AddLimitEventAsync(new LimitEvent
        {
            WorkspaceId = workspace.Id,
            UsageCycleId = previousCycle.Id,
            Dimension = BillingDimension.EstimatedCost,
            EventType = LimitEventType.WarningRaised,
            TriggeredBySourceType = "AIUsageEvent",
            TriggeredBySourceId = Guid.NewGuid().ToString(),
            CurrentQuantity = 123.40m,
            ThresholdQuantity = 120m,
            Reason = "Warning"
        });
        await store.AddLimitEventAsync(new LimitEvent
        {
            WorkspaceId = workspace.Id,
            UsageCycleId = previousCycle.Id,
            Dimension = BillingDimension.AIActivityEvents,
            EventType = LimitEventType.RestrictionApplied,
            TriggeredBySourceType = "Reconciliation",
            TriggeredBySourceId = Guid.NewGuid().ToString(),
            CurrentQuantity = 5_235m,
            ThresholdQuantity = 5_000m,
            Reason = "Restriction"
        });
        await store.AddCycleAdjustmentAsync(new CycleAdjustment
        {
            UsageCycleId = previousCycle.Id,
            Dimension = BillingDimension.AIActivityEvents,
            DeltaQuantity = 234m,
            AdjustmentType = CycleAdjustmentType.LateActivity,
            SourceReference = "reconciliation:1",
            Reason = "Late activity"
        });

        return (workspace, owner, previousCycle.Id);
    }

    private static IReadOnlyList<UsageCycleMetric> CreateMetrics(
        Guid cycleId,
        decimal activeMembers,
        decimal aiActivity,
        decimal estimatedCost,
        decimal aiOverage,
        decimal costOverage)
    {
        return
        [
            new UsageCycleMetric
            {
                UsageCycleId = cycleId,
                Dimension = BillingDimension.ActiveMembers,
                IncludedQuantity = 10m,
                WarningThresholdQuantity = 8m,
                HardLimitQuantity = 10m,
                CurrentQuantity = activeMembers,
                LimitBehavior = BillingLimitBehavior.Restrict,
                State = activeMembers >= 8m ? UsageCycleMetricState.Warning : UsageCycleMetricState.WithinLimit
            },
            new UsageCycleMetric
            {
                UsageCycleId = cycleId,
                Dimension = BillingDimension.AIActivityEvents,
                IncludedQuantity = 5_000m,
                WarningThresholdQuantity = 4_500m,
                CurrentQuantity = aiActivity,
                OverageQuantity = aiOverage,
                LimitBehavior = BillingLimitBehavior.AllowOverage,
                State = aiOverage > 0m ? UsageCycleMetricState.Overage : UsageCycleMetricState.WithinLimit
            },
            new UsageCycleMetric
            {
                UsageCycleId = cycleId,
                Dimension = BillingDimension.EstimatedCost,
                IncludedQuantity = 150m,
                WarningThresholdQuantity = 120m,
                CurrentQuantity = estimatedCost,
                OverageQuantity = costOverage,
                LimitBehavior = BillingLimitBehavior.AllowOverage,
                State = costOverage > 0m ? UsageCycleMetricState.Overage : UsageCycleMetricState.Warning
            }
        ];
    }
}
