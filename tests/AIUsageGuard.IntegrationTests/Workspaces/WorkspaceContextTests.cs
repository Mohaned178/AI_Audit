using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Workspaces;

public sealed class WorkspaceContextTests
{
    [Fact]
    public async Task Workspace_context_returns_owner_context_for_authenticated_user()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var context = await client.GetWorkspaceContextAsync(session.WorkspaceId);

        Assert.Equal(session.WorkspaceId, context.WorkspaceId);
        Assert.Equal("Owner", context.CurrentRole);
    }
}
