using System.Net;
using AIUsageGuard.Api.Contracts.Reporting;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Reporting;

public sealed class ReportingFailureTests
{
    [Fact]
    public async Task Dashboard_rejects_reversed_period()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var response = await client.GetDashboardResponseAsync(
            session.WorkspaceId,
            new ReportingPeriodRequest(new DateOnly(2026, 4, 7), new DateOnly(2026, 4, 1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cost_summary_rejects_reversed_period()
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
            new ReportingPeriodRequest(new DateOnly(2026, 4, 7), new DateOnly(2026, 4, 1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
