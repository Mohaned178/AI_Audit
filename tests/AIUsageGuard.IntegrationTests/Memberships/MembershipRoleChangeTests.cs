using System.Net;
using System.Net.Http.Json;
using AIUsageGuard.Api.Contracts.Memberships;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Memberships;

public sealed class MembershipRoleChangeTests
{
    [Fact]
    public async Task Owner_can_promote_member_to_admin()
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
        var response = await ownerClient.PatchAsJsonAsync(
            $"/workspaces/{owner.WorkspaceId}/memberships/{membership.MembershipId}",
            new UpdateMembershipRequest(AIUsageGuard.Application.Models.WorkspaceRole.Admin, null));

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<MembershipResponse>();

        Assert.NotNull(updated);
        Assert.Equal(AIUsageGuard.Application.Models.WorkspaceRole.Admin, updated!.Role);
    }

    [Fact]
    public async Task Owner_cannot_remove_the_last_owner()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();

        var owner = await ownerClient.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var ownerMembership = await factory.ExecuteDbContextAsync(dbContext =>
            dbContext.WorkspaceMemberships.SingleAsync(membership => membership.UserId == owner.UserId));
        var response = await ownerClient.PatchAsJsonAsync(
            $"/workspaces/{owner.WorkspaceId}/memberships/{ownerMembership.Id}",
            new UpdateMembershipRequest(AIUsageGuard.Application.Models.WorkspaceRole.Admin, null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
