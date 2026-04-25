using System.Net;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Billing;

public sealed class BillingCycleAuthorizationTests
{
    [Fact]
    public async Task Member_cannot_access_billing_cycle_history()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        using var memberClient = await factory.CreateInitializedApiClientAsync();

        var owner = await ownerClient.RegisterWorkspaceAsync("owner@example.com", "Password123!", "Owner", "Alpha Workspace");
        var member = await memberClient.RegisterWorkspaceAsync("member@example.com", "Password123!", "Member", "Beta Workspace");
        await ownerClient.CreateMembershipAsync(owner.WorkspaceId, "member@example.com", "Member");
        var scenario = await BillingTestData.SeedBillingHistoryScenarioAsync(factory, owner);

        var listResponse = await memberClient.ListBillingCyclesResponseAsync(owner.WorkspaceId);
        var detailResponse = await memberClient.GetBillingCycleResponseAsync(owner.WorkspaceId, scenario.PreviousCycleId);

        Assert.Equal(HttpStatusCode.Forbidden, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, detailResponse.StatusCode);

        var deniedAuditCount = await factory.ExecuteDbContextAsync(dbContext =>
            dbContext.AuditRecords.CountAsync(item => item.Result == "denied" &&
                                                      (item.ActionType == "billing.cycle_history.read" || item.ActionType == "billing.cycle.read")));
        Assert.True(deniedAuditCount >= 1);
        _ = member;
    }

    [Fact]
    public async Task Admin_from_another_workspace_cannot_access_billing_cycle_history()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        using var outsiderClient = await factory.CreateInitializedApiClientAsync();

        var owner = await ownerClient.RegisterWorkspaceAsync("owner@example.com", "Password123!", "Owner", "Alpha Workspace");
        var outsider = await outsiderClient.RegisterWorkspaceAsync("outsider@example.com", "Password123!", "Outsider", "Beta Workspace");
        var scenario = await BillingTestData.SeedBillingHistoryScenarioAsync(factory, owner);

        var listResponse = await outsiderClient.ListBillingCyclesResponseAsync(owner.WorkspaceId);
        var detailResponse = await outsiderClient.GetBillingCycleResponseAsync(owner.WorkspaceId, scenario.PreviousCycleId);

        Assert.Equal(HttpStatusCode.Forbidden, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, detailResponse.StatusCode);
        _ = outsider;
    }
}
