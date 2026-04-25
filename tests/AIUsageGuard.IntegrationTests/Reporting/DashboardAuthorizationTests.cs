using System.Net;
using AIUsageGuard.Api.Contracts.Reporting;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Reporting;

public sealed class DashboardAuthorizationTests
{
    [Fact]
    public async Task Non_admin_member_cannot_view_dashboard_summary()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        using var memberClient = await factory.CreateInitializedApiClientAsync();

        var ownerSession = await ownerClient.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        await memberClient.RegisterWorkspaceAsync(
            "member@example.com",
            "Password123!",
            "Member",
            "Beta Workspace");

        await ownerClient.CreateMembershipAsync(ownerSession.WorkspaceId, "member@example.com", "Member");

        var response = await memberClient.GetDashboardResponseAsync(
            ownerSession.WorkspaceId,
            new ReportingPeriodRequest(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var deniedAuditCount = await factory.ExecuteDbContextAsync(async dbContext =>
            await dbContext.AuditRecords.CountAsync(item => item.ActionType == "dashboard.read" && item.Result == "denied"));

        Assert.True(deniedAuditCount >= 1);
    }

    [Fact]
    public async Task Admin_cannot_view_another_workspace_dashboard()
    {
        await using var factory = new TestWebApplicationFactory();
        using var firstClient = await factory.CreateInitializedApiClientAsync();
        using var secondClient = await factory.CreateInitializedApiClientAsync();

        var firstSession = await firstClient.RegisterWorkspaceAsync(
            "owner1@example.com",
            "Password123!",
            "Owner 1",
            "Alpha Workspace");

        var secondSession = await secondClient.RegisterWorkspaceAsync(
            "owner2@example.com",
            "Password123!",
            "Owner 2",
            "Beta Workspace");

        var response = await secondClient.GetDashboardResponseAsync(
            firstSession.WorkspaceId,
            new ReportingPeriodRequest(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(firstSession.WorkspaceId, secondSession.WorkspaceId);
    }
}
