using AIUsageGuard.Application.Billing.GetPlanStatus;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageGuard.UnitTests.Billing;

public sealed class GetPlanStatusServiceTests
{
    [Fact]
    public async Task Get_returns_current_plan_status_snapshot_and_records_audit()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var cycleStartUtc = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var cycleEndUtc = cycleStartUtc.AddMonths(1);

        await SeedWorkspaceAsync(dbContext, workspaceId, ownerId);

        var starterPlan = new PlanDefinition
        {
            PlanCode = "starter",
            DisplayName = "Starter",
            IsDefault = true
        };
        var growthPlan = new PlanDefinition
        {
            PlanCode = "growth",
            DisplayName = "Growth"
        };
        dbContext.PlanDefinitions.AddRange(starterPlan, growthPlan);

        var currentAssignment = new WorkspacePlanAssignment
        {
            WorkspaceId = workspaceId,
            PlanDefinitionId = starterPlan.Id,
            EffectiveFromCycleStartUtc = cycleStartUtc,
            AssignedByUserId = ownerId,
            ChangeReason = "Initial assignment"
        };
        var nextAssignment = new WorkspacePlanAssignment
        {
            WorkspaceId = workspaceId,
            PlanDefinitionId = growthPlan.Id,
            EffectiveFromCycleStartUtc = cycleEndUtc,
            AssignedByUserId = ownerId,
            ChangeReason = "Scheduled upgrade"
        };
        dbContext.WorkspacePlanAssignments.AddRange(currentAssignment, nextAssignment);

        var currentCycle = new UsageCycle
        {
            WorkspaceId = workspaceId,
            CycleStartUtc = cycleStartUtc,
            CycleEndExclusiveUtc = cycleEndUtc,
            PlanAssignmentId = currentAssignment.Id,
            Status = UsageCycleStatus.Open,
            OpenedAtUtc = cycleStartUtc,
            LastCalculatedAtUtc = cycleStartUtc.AddDays(20)
        };
        dbContext.UsageCycles.Add(currentCycle);
        dbContext.UsageCycleMetrics.AddRange(
            new UsageCycleMetric
            {
                UsageCycleId = currentCycle.Id,
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
                UsageCycleId = currentCycle.Id,
                Dimension = BillingDimension.AIActivityEvents,
                IncludedQuantity = 5_000m,
                WarningThresholdQuantity = 4_500m,
                CurrentQuantity = 4_120m,
                LimitBehavior = BillingLimitBehavior.AllowOverage,
                State = UsageCycleMetricState.WithinLimit
            },
            new UsageCycleMetric
            {
                UsageCycleId = currentCycle.Id,
                Dimension = BillingDimension.EstimatedCost,
                IncludedQuantity = 150m,
                WarningThresholdQuantity = 120m,
                CurrentQuantity = 132.45m,
                LimitBehavior = BillingLimitBehavior.AllowOverage,
                State = UsageCycleMetricState.Warning
            });
        await dbContext.SaveChangesAsync();

        var service = new GetPlanStatusService(
            dbContext,
            new AuditService(dbContext),
            BillingTestFactory.CreatePlanAssignmentService(dbContext),
            NullLogger<GetPlanStatusService>.Instance);

        var result = await service.GetAsync(new GetPlanStatusQuery(workspaceId, ownerId));

        Assert.Equal(workspaceId, result.Snapshot.WorkspaceId);
        Assert.Equal("starter", result.Snapshot.PlanCode);
        Assert.Equal("growth", result.Snapshot.NextPlanCode);
        Assert.Equal(cycleEndUtc, result.Snapshot.NextPlanStartsAtUtc);
        Assert.Equal(3, result.Snapshot.MetricStatuses.Count);
        Assert.Equal(cycleStartUtc, result.AssignedFromCycleStartUtc);
        Assert.Equal(1, await dbContext.AuditRecords.CountAsync(item => item.ActionType == "billing.plan_status.read" && item.Result == "success"));
    }

    private static async Task SeedWorkspaceAsync(ApplicationDbContext dbContext, Guid workspaceId, Guid ownerId)
    {
        dbContext.Workspaces.Add(new Workspace
        {
            Id = workspaceId,
            Name = "Alpha Workspace",
            Slug = $"alpha-{workspaceId:N}",
            CreatedByUserId = ownerId
        });

        await dbContext.AddUserAsync(new UserAccount
        {
            Id = ownerId,
            Email = $"owner-{ownerId:N}@example.com",
            DisplayName = "Owner",
            PasswordHash = "hash"
        });
        await dbContext.AddMembershipAsync(new WorkspaceMembership
        {
            WorkspaceId = workspaceId,
            UserId = ownerId,
            Role = WorkspaceRole.Owner,
            Status = MembershipStatus.Active
        });
    }
}
