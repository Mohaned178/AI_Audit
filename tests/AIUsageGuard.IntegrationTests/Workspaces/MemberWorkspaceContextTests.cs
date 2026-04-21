using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Workspaces;

public sealed class MemberWorkspaceContextTests
{
    [Fact]
    public async Task Member_can_read_workspace_context_for_assigned_workspace()
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
        await memberClient.LogoutAsync();
        await memberClient.LoginAsync("member@example.com", "Password123!");

        var context = await memberClient.GetWorkspaceContextAsync(owner.WorkspaceId);

        Assert.Equal(owner.WorkspaceId, context.WorkspaceId);
        Assert.Equal("Member", context.CurrentRole);
    }
}
