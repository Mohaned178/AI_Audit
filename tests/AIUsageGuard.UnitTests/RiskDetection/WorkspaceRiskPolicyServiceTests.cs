using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.RiskDetection.GetRiskPolicy;
using AIUsageGuard.Application.RiskDetection.UpdateRiskPolicy;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.UnitTests.RiskDetection;

public sealed class WorkspaceRiskPolicyServiceTests
{
    [Fact]
    public async Task Update_normalizes_tools_and_persists_policy()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        await SeedWorkspaceAsync(dbContext, workspaceId, actorId);
        var service = new UpdateWorkspaceRiskPolicyService(dbContext, new AuditService(dbContext));

        var result = await service.UpdateAsync(new UpdateWorkspaceRiskPolicyCommand(
            workspaceId,
            actorId,
            [" Claude ", "ChatGPT", "claude"],
            3m,
            15m));

        Assert.True(result.CreatedNewPolicy);
        Assert.Equal(["ChatGPT", "Claude"], result.Policy.ApprovedTools);
        Assert.Equal(1, await dbContext.WorkspaceRiskPolicies.CountAsync());
        Assert.True(await dbContext.AuditRecords.AnyAsync(item => item.ActionType == "risk_policy.update" && item.Result == "success"));
    }

    [Fact]
    public async Task Update_rejects_negative_threshold_values()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        await SeedWorkspaceAsync(dbContext, workspaceId, actorId);
        var service = new UpdateWorkspaceRiskPolicyService(dbContext, new AuditService(dbContext));

        var exception = await Assert.ThrowsAsync<RequestFailureException>(() =>
            service.UpdateAsync(new UpdateWorkspaceRiskPolicyCommand(
                workspaceId,
                actorId,
                ["ChatGPT"],
                -1m,
                null)));

        Assert.Equal(400, exception.StatusCode);
        Assert.True(await dbContext.AuditRecords.AnyAsync(item => item.ActionType == "risk_policy.update" && item.Result == "failed"));
    }

    [Fact]
    public async Task Get_returns_default_workspace_policy_when_none_exists()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        await SeedWorkspaceAsync(dbContext, workspaceId, actorId);
        var service = new GetWorkspaceRiskPolicyService(dbContext, new AuditService(dbContext));

        var result = await service.GetAsync(new GetWorkspaceRiskPolicyQuery(workspaceId, actorId));

        Assert.Equal(workspaceId, result.Policy.WorkspaceId);
        Assert.Empty(result.Policy.ApprovedTools);
        Assert.True(await dbContext.AuditRecords.AnyAsync(item => item.ActionType == "risk_policy.read" && item.Result == "success"));
    }

    private static async Task SeedWorkspaceAsync(ApplicationDbContext dbContext, Guid workspaceId, Guid actorId)
    {
        dbContext.Workspaces.Add(new Workspace
        {
            Id = workspaceId,
            Name = "Alpha Workspace",
            Slug = $"alpha-{workspaceId:N}",
            CreatedByUserId = actorId
        });
        await dbContext.SaveChangesAsync();
    }
}
