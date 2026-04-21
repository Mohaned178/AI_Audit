using System.Net.Http.Json;
using AIUsageGuard.Api.Contracts.Memberships;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Memberships;

public sealed class MembershipListTests
{
    [Fact]
    public async Task Owner_can_list_workspace_memberships()
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

        await ownerClient.CreateMembershipAsync(owner.WorkspaceId, "member@example.com", "Member");

        var response = await ownerClient.GetAsync($"/workspaces/{owner.WorkspaceId}/memberships");
        response.EnsureSuccessStatusCode();
        var memberships = await response.Content.ReadFromJsonAsync<IReadOnlyList<MembershipResponse>>();

        Assert.NotNull(memberships);
        Assert.Equal(2, memberships!.Count);
    }
}
