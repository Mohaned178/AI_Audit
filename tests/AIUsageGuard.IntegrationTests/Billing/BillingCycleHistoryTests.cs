using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Billing;

public sealed class BillingCycleHistoryTests
{
    [Fact]
    public async Task Owner_can_list_and_open_billing_cycle_history()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        var owner = await ownerClient.RegisterWorkspaceAsync("owner@example.com", "Password123!", "Owner", "Alpha Workspace");
        var scenario = await BillingTestData.SeedBillingHistoryScenarioAsync(factory, owner);

        var list = await ownerClient.ListBillingCyclesAsync(owner.WorkspaceId);
        var detail = await ownerClient.GetBillingCycleAsync(owner.WorkspaceId, scenario.PreviousCycleId);

        Assert.Equal(2, list.Page.Items.Count);
        Assert.Equal("adjusted", detail.Cycle.Status);
        Assert.Equal("late_activity", detail.Adjustments.Single().AdjustmentType);
    }
}
