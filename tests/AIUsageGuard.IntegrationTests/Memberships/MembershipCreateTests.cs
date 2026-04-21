using System.Net;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Memberships;

public sealed class MembershipCreateTests
{
    [Fact]
    public async Task Owner_can_add_member_to_workspace()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        using var memberClient = await factory.CreateInitializedApiClientAsync();

        var owner = await ownerClient.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        await memberClient.RegisterWorkspaceAsync(
            "member@example.com",
            "Password123!",
            "Member",
            "Beta Workspace");

        var membership = await ownerClient.CreateMembershipAsync(owner.WorkspaceId, "member@example.com", "Member");

        Assert.Equal(owner.WorkspaceId, membership.WorkspaceId);
        Assert.Equal("member@example.com", membership.UserEmail);
        Assert.Equal(3, await factory.ExecuteDbContextAsync(dbContext => dbContext.WorkspaceMemberships.CountAsync()));
        Assert.Equal(2, await factory.ExecuteDbContextAsync(dbContext => dbContext.WorkspaceMemberships.CountAsync(item => item.WorkspaceId == owner.WorkspaceId)));
        Assert.True(await factory.ExecuteDbContextAsync(dbContext => dbContext.AuditRecords.AnyAsync(audit => audit.ActionType == "membership.create")));
    }
}
