using AIUsageGuard.Api.Contracts.Reporting;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Reporting;

public sealed class GetCostSummaryTests
{
    [Fact]
    public async Task Owner_can_retrieve_partial_cost_summary_for_selected_period()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        using var memberClient = await factory.CreateInitializedApiClientAsync();
        using var outsiderClient = await factory.CreateInitializedApiClientAsync();

        var owner = await ownerClient.RegisterWorkspaceAsync("owner@example.com", "Password123!", "Owner", "Alpha Workspace");
        var member = await memberClient.RegisterWorkspaceAsync("member@example.com", "Password123!", "Member", "Beta Workspace");
        var outsider = await outsiderClient.RegisterWorkspaceAsync("outsider@example.com", "Password123!", "Outsider", "Gamma Workspace");
        await ownerClient.CreateMembershipAsync(owner.WorkspaceId, "member@example.com", "Member");
        await ReportingTestData.SeedScenarioAsync(factory, owner, member, outsider);

        var response = await ownerClient.GetCostSummaryAsync(
            owner.WorkspaceId,
            new ReportingPeriodRequest(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7)));

        Assert.Equal(15m, response.Summary.EstimatedCostTotal);
        Assert.Equal(2, response.Summary.EventsWithEstimatedCost);
        Assert.Equal(1, response.Summary.EventsMissingEstimatedCost);
        Assert.True(response.Summary.IsPartial);
        Assert.Equal(7, response.Summary.DailyTrend.Count);
    }

    [Fact]
    public async Task Empty_period_returns_zeroed_cost_summary()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var response = await client.GetCostSummaryAsync(
            session.WorkspaceId,
            new ReportingPeriodRequest(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7)));

        Assert.Equal(0m, response.Summary.EstimatedCostTotal);
        Assert.Equal(0, response.Summary.EventsWithEstimatedCost);
        Assert.Equal(0, response.Summary.EventsMissingEstimatedCost);
        Assert.False(response.Summary.IsPartial);
        Assert.Equal(7, response.Summary.DailyTrend.Count);
    }
}
