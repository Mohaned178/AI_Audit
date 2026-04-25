using System.Net;
using AIUsageGuard.Api.Contracts.Reporting;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Reporting;

public sealed class ReportingAuthorizationTests
{
    [Fact]
    public async Task Member_cannot_access_grouped_reports()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        using var memberClient = await factory.CreateInitializedApiClientAsync();

        var owner = await ownerClient.RegisterWorkspaceAsync("owner@example.com", "Password123!", "Owner", "Alpha Workspace");
        await memberClient.RegisterWorkspaceAsync("member@example.com", "Password123!", "Member", "Beta Workspace");
        await ownerClient.CreateMembershipAsync(owner.WorkspaceId, "member@example.com", "Member");

        var response = await memberClient.GetUsageByUserResponseAsync(
            owner.WorkspaceId,
            new PagedReportingRequest(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7), 1, 20));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var deniedAuditCount = await factory.ExecuteDbContextAsync(async dbContext =>
            await dbContext.AuditRecords.CountAsync(item => item.Result == "denied" && item.ActionType == "report.usage_by_user.read"));

        Assert.True(deniedAuditCount >= 1);
    }

    [Fact]
    public async Task Admin_cannot_access_another_workspace_alerts_summary()
    {
        await using var factory = new TestWebApplicationFactory();
        using var firstClient = await factory.CreateInitializedApiClientAsync();
        using var secondClient = await factory.CreateInitializedApiClientAsync();

        var first = await firstClient.RegisterWorkspaceAsync("owner1@example.com", "Password123!", "Owner 1", "Alpha Workspace");
        var second = await secondClient.RegisterWorkspaceAsync("owner2@example.com", "Password123!", "Owner 2", "Beta Workspace");

        var response = await secondClient.GetAlertsSummaryResponseAsync(
            first.WorkspaceId,
            new ReportingPeriodRequest(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(first.WorkspaceId, second.WorkspaceId);
    }
}
