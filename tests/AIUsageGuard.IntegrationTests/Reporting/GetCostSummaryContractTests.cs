using System.Net;
using AIUsageGuard.Api.Contracts.Reporting;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Reporting;

public sealed class GetCostSummaryContractTests
{
    [Fact]
    public async Task Cost_summary_returns_period_and_summary_contract_fields()
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

        Assert.Equal(owner.WorkspaceId, response.WorkspaceId);
        Assert.Equal(7, response.Period.DayCount);
        Assert.True(response.Summary.EstimatedCostTotal >= 0m);
        Assert.NotNull(response.Summary.DailyTrend);
    }

    [Fact]
    public async Task Cost_summary_rejects_unsupported_range_with_problem_details()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var response = await client.GetCostSummaryResponseAsync(
            session.WorkspaceId,
            new ReportingPeriodRequest(new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30)));

        var problem = await response.ReadProblemAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(400, problem.Status);
        Assert.Contains("93", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }
}
