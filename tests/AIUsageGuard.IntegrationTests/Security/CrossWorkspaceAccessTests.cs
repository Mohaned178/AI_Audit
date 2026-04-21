using System.Net;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Security;

public sealed class CrossWorkspaceAccessTests
{
    [Fact]
    public async Task User_cannot_access_context_for_workspace_they_do_not_belong_to()
    {
        await using var factory = new TestWebApplicationFactory();
        using var firstClient = await factory.CreateInitializedApiClientAsync();
        using var secondClient = await factory.CreateInitializedApiClientAsync();

        await firstClient.RegisterWorkspaceAsync(
            "owner1@example.com",
            "Password123!",
            "Owner 1",
            "Alpha Workspace");

        var second = await secondClient.RegisterWorkspaceAsync(
            "owner2@example.com",
            "Password123!",
            "Owner 2",
            "Beta Workspace");

        var response = await firstClient.GetAsync($"/workspaces/{second.WorkspaceId}/context");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
