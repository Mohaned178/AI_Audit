using AIUsageGuard.Api.Contracts.Reporting;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Reporting;

public sealed class ReportingRegressionTests
{
    [Fact]
    public async Task Dashboard_grouped_reports_and_cost_summary_stay_consistent()
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

        var period = new ReportingPeriodRequest(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7));
        var dashboard = await ownerClient.GetDashboardAsync(owner.WorkspaceId, period);
        var usageByUser = await ownerClient.GetUsageByUserAsync(owner.WorkspaceId, new PagedReportingRequest(period.FromDate, period.ToDate, 1, 20));
        var usageByTool = await ownerClient.GetUsageByToolAsync(owner.WorkspaceId, new PagedReportingRequest(period.FromDate, period.ToDate, 1, 20));
        var costSummary = await ownerClient.GetCostSummaryAsync(owner.WorkspaceId, period);

        Assert.Equal(dashboard.Totals.TotalEvents, usageByUser.Page.Items.Sum(item => item.TotalEvents));
        Assert.Equal(dashboard.Totals.TotalEvents, usageByTool.Page.Items.Sum(item => item.TotalEvents));
        Assert.Equal(dashboard.Costs.EstimatedCostTotal, costSummary.Summary.EstimatedCostTotal);
        Assert.Equal(dashboard.Alerts.TotalFindings, usageByUser.Page.Items.Sum(item => item.FlaggedFindingCount));
    }
}
