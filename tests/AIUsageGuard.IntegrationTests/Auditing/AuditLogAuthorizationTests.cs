using System.Net;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Auditing;

public sealed class AuditLogAuthorizationTests
{
    [Fact]
    public async Task Member_cannot_list_workspace_audit_logs()
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

        var response = await memberClient.ListAuditLogsResponseAsync(ownerSession.WorkspaceId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            Assert.True(await dbContext.AuditRecords.AnyAsync(item =>
                item.WorkspaceId == ownerSession.WorkspaceId &&
                item.ActionType == "audit_log.list" &&
                item.Result == "denied"));
        });
    }

    [Fact]
    public async Task Cross_workspace_owner_cannot_read_audit_log_detail()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        using var otherOwnerClient = await factory.CreateInitializedApiClientAsync();

        var ownerSession = await ownerClient.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var otherWorkspaceSession = await otherOwnerClient.RegisterWorkspaceAsync(
            "other@example.com",
            "Password123!",
            "Other Owner",
            "Beta Workspace");

        var response = await otherOwnerClient.GetAuditLogResponseAsync(ownerSession.WorkspaceId, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            Assert.True(await dbContext.AuditRecords.AnyAsync(item =>
                item.WorkspaceId == ownerSession.WorkspaceId &&
                item.ActionType == "audit_log.read" &&
                item.Result == "denied"));
        });

        Assert.NotEqual(ownerSession.WorkspaceId, otherWorkspaceSession.WorkspaceId);
    }
}
