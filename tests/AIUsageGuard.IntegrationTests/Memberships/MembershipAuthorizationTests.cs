using System.Net;
using System.Net.Http.Json;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Memberships;

public sealed class MembershipAuthorizationTests
{
    [Fact]
    public async Task Member_cannot_create_membership()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        using var memberClient = await factory.CreateInitializedApiClientAsync();
        using var outsiderClient = await factory.CreateInitializedApiClientAsync();

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

        await outsiderClient.RegisterWorkspaceAsync(
            "outsider@example.com",
            "Password123!",
            "Outsider",
            "Gamma Workspace");

        await ownerClient.CreateMembershipAsync(owner.WorkspaceId, "member@example.com", "Member");
        await memberClient.LogoutAsync();
        await memberClient.LoginAsync("member@example.com", "Password123!");

        var response = await memberClient.PostAsJsonAsync(
            $"/workspaces/{owner.WorkspaceId}/memberships",
            new AIUsageGuard.Api.Contracts.Memberships.CreateMembershipRequest(
                "outsider@example.com",
                AIUsageGuard.Application.Models.WorkspaceRole.Member));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
