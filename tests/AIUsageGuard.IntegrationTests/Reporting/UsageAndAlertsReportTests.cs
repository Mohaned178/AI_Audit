using AIUsageGuard.Api.Contracts.Reporting;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Reporting;

public sealed class UsageAndAlertsReportTests
{
    [Fact]
    public async Task Owner_can_retrieve_grouped_usage_and_alert_summaries()
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

        var usageByUser = await ownerClient.GetUsageByUserAsync(
            owner.WorkspaceId,
            new PagedReportingRequest(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7), 1, 20));
        var usageByTool = await ownerClient.GetUsageByToolAsync(
            owner.WorkspaceId,
            new PagedReportingRequest(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7), 1, 20));
        var alerts = await ownerClient.GetAlertsSummaryAsync(
            owner.WorkspaceId,
            new ReportingPeriodRequest(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7)));

        Assert.Equal(2, usageByUser.Page.TotalCount);
        Assert.Equal("Owner", usageByUser.Page.Items[0].DisplayLabel);
        Assert.Equal(2, usageByTool.Page.TotalCount);
        Assert.Equal("ChatGPT", usageByTool.Page.Items[0].DisplayLabel);
        Assert.Equal(2, alerts.Summary.TotalFindings);
        Assert.Equal(1, alerts.Summary.HighSeverityCount);
        Assert.Equal(1, alerts.Summary.MediumSeverityCount);
    }
}
