using AIUsageGuard.Application.Billing.ApplyWorkspacePlanAssignment;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.UnitTests.Billing;

public sealed class ApplyWorkspacePlanAssignmentServiceTests
{
    [Fact]
    public async Task EnsureCurrentCycle_creates_default_assignment_and_cycle()
    {
        await using var store = TestDbContextFactory.CreateContext();
        var (workspace, owner) = await SeedWorkspaceAsync(store);
        var service = BillingTestFactory.CreatePlanAssignmentService(store);

        var cycle = await service.EnsureCurrentCycleAsync(workspace.Id, owner.Id, new DateTimeOffset(2026, 4, 23, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(workspace.Id, cycle.WorkspaceId);
        Assert.Equal(1, await store.WorkspacePlanAssignments.CountAsync(item => item.WorkspaceId == workspace.Id));
        Assert.Equal(3, await store.UsageCycleMetrics.CountAsync(item => item.UsageCycleId == cycle.Id));
    }

    [Fact]
    public async Task ApplyAsync_schedules_future_plan_without_opening_current_cycle()
    {
        await using var store = TestDbContextFactory.CreateContext();
        var (workspace, owner) = await SeedWorkspaceAsync(store);
        var service = BillingTestFactory.CreatePlanAssignmentService(store);
        await service.EnsureCurrentCycleAsync(workspace.Id, owner.Id, new DateTimeOffset(2026, 4, 23, 0, 0, 0, TimeSpan.Zero));

        var growthPlan = new PlanDefinition
        {
            PlanCode = "growth",
            DisplayName = "Growth",
            IsActive = true
        };
        await store.AddPlanDefinitionAsync(growthPlan);
        await store.AddPlanLimitRuleAsync(new PlanLimitRule
        {
            PlanDefinitionId = growthPlan.Id,
            Dimension = BillingDimension.ActiveMembers,
            IncludedQuantity = 50m,
            WarningThresholdQuantity = 40m,
            HardLimitQuantity = 50m,
            LimitBehavior = BillingLimitBehavior.Restrict
        });

        var result = await service.ApplyAsync(
            new ApplyWorkspacePlanAssignmentCommand(
                workspace.Id,
                owner.Id,
                "growth",
                new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
                "Scheduled upgrade"));

        Assert.True(result.ScheduledForFutureCycle);
        Assert.Null(result.OpenedCycle);
        Assert.Equal(2, await store.WorkspacePlanAssignments.CountAsync(item => item.WorkspaceId == workspace.Id));
    }

    private static async Task<(Workspace Workspace, UserAccount Owner)> SeedWorkspaceAsync(ApplicationDbContext store)
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
        return (workspace, owner);
    }
}
