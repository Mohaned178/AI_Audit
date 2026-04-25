using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Billing;

public sealed class PlanStatusTests
{
    [Fact]
    public async Task Owner_can_retrieve_current_workspace_plan_status()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();

        var owner = await ownerClient.RegisterWorkspaceAsync("owner@example.com", "Password123!", "Owner", "Alpha Workspace");
        await BillingTestData.SeedPlanStatusScenarioAsync(factory, owner);

        var response = await ownerClient.GetPlanStatusAsync(owner.WorkspaceId);

        Assert.Equal("Starter", response.Plan.DisplayName);
        Assert.Equal(8m, response.Metrics.Single(item => item.Dimension == "active_members").CurrentQuantity);
        Assert.Equal(2m, response.Metrics.Single(item => item.Dimension == "active_members").RemainingQuantity);
        Assert.Equal(132.45m, response.Metrics.Single(item => item.Dimension == "estimated_cost").CurrentQuantity);
    }
}
