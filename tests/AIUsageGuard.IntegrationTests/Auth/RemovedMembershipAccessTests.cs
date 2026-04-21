using System.Net;
using System.Net.Http.Json;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Auth;

public sealed class RemovedMembershipAccessTests
{
    [Fact]
    public async Task Login_fails_when_only_membership_is_removed()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "member@example.com",
            "Password123!",
            "Member",
            "Beta Workspace");

        var membership = await factory.ExecuteDbContextAsync(dbContext =>
            dbContext.WorkspaceMemberships.SingleAsync(item => item.UserId == session.UserId));
        membership.Status = AIUsageGuard.Application.Models.MembershipStatus.Removed;
        await factory.ExecuteDbContextAsync(dbContext => dbContext.UpdateMembershipAsync(membership));

        await client.LogoutAsync();

        var response = await client.PostAsJsonAsync("/auth/login", new AIUsageGuard.Api.Contracts.Auth.LoginRequest("member@example.com", "Password123!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
