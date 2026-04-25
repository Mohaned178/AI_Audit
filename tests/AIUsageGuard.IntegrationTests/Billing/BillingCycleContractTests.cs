using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Billing;

public sealed class BillingCycleContractTests
{
    [Fact]
    public async Task Billing_cycle_endpoints_match_expected_contract_shape()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        var owner = await ownerClient.RegisterWorkspaceAsync("owner@example.com", "Password123!", "Owner", "Alpha Workspace");
        var scenario = await BillingTestData.SeedBillingHistoryScenarioAsync(factory, owner);

        var list = await ownerClient.ListBillingCyclesAsync(owner.WorkspaceId);
        var detail = await ownerClient.GetBillingCycleAsync(owner.WorkspaceId, scenario.PreviousCycleId);

        Assert.Equal(owner.WorkspaceId, list.WorkspaceId);
        Assert.Equal(2, list.Page.TotalCount);
        Assert.Equal("starter", detail.Cycle.PlanCode);
        Assert.Equal(2, detail.LimitEvents.Count);
        Assert.Single(detail.Adjustments);
    }
}
