using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Billing;

public sealed class GetPlanStatusContractTests
{
    [Fact]
    public async Task Plan_status_returns_expected_contract_shape()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();

        var owner = await ownerClient.RegisterWorkspaceAsync("owner@example.com", "Password123!", "Owner", "Alpha Workspace");
        await BillingTestData.SeedPlanStatusScenarioAsync(factory, owner);

        var response = await ownerClient.GetPlanStatusAsync(owner.WorkspaceId);

        Assert.Equal(owner.WorkspaceId, response.WorkspaceId);
        Assert.Equal("starter", response.Plan.PlanCode);
        Assert.Equal("growth", response.Plan.NextPlanCode);
        Assert.Equal("open", response.CurrentCycle.Status);
        Assert.Equal(3, response.Metrics.Count);
        Assert.Contains(response.Metrics, item => item.Dimension == "active_members" && item.State == "warning");
        Assert.Contains(response.Metrics, item => item.Dimension == "ai_activity_events" && item.LimitBehavior == "allow_overage");
    }
}
