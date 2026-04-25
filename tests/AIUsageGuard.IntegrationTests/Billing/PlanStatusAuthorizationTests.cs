using System.Net;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Billing;

public sealed class PlanStatusAuthorizationTests
{
    [Fact]
    public async Task Member_cannot_access_plan_status()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        using var memberClient = await factory.CreateInitializedApiClientAsync();

        var owner = await ownerClient.RegisterWorkspaceAsync("owner@example.com", "Password123!", "Owner", "Alpha Workspace");
        await memberClient.RegisterWorkspaceAsync("member@example.com", "Password123!", "Member", "Beta Workspace");
        await ownerClient.CreateMembershipAsync(owner.WorkspaceId, "member@example.com", "Member");
        await BillingTestData.SeedPlanStatusScenarioAsync(factory, owner);

        var response = await memberClient.GetPlanStatusResponseAsync(owner.WorkspaceId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var deniedAuditCount = await factory.ExecuteDbContextAsync(async dbContext =>
            await dbContext.AuditRecords.CountAsync(item => item.Result == "denied" && item.ActionType == "billing.plan_status.read"));

        Assert.True(deniedAuditCount >= 1);
    }

    [Fact]
    public async Task Admin_from_another_workspace_cannot_access_plan_status()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        using var outsiderClient = await factory.CreateInitializedApiClientAsync();

        var owner = await ownerClient.RegisterWorkspaceAsync("owner@example.com", "Password123!", "Owner", "Alpha Workspace");
        await outsiderClient.RegisterWorkspaceAsync("outsider@example.com", "Password123!", "Outsider", "Beta Workspace");
        await BillingTestData.SeedPlanStatusScenarioAsync(factory, owner);

        var response = await outsiderClient.GetPlanStatusResponseAsync(owner.WorkspaceId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
