using System.Net;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Notifications;

public sealed class NotificationAuthorizationTests
{
    [Fact]
    public async Task Members_and_cross_workspace_operators_are_denied_notification_administration()
    {
        await using var factory = new TestWebApplicationFactory();
        using var alphaOwnerClient = await factory.CreateInitializedApiClientAsync();
        using var betaOwnerClient = await factory.CreateInitializedApiClientAsync();

        var alpha = await alphaOwnerClient.RegisterWorkspaceAsync(
            "alpha-owner@example.com",
            "Password123!",
            "Alpha Owner",
            "Alpha Workspace");
        var beta = await betaOwnerClient.RegisterWorkspaceAsync(
            "beta-owner@example.com",
            "Password123!",
            "Beta Owner",
            "Beta Workspace");

        var crossWorkspaceResponse = await betaOwnerClient.GetAsync($"/workspaces/{alpha.WorkspaceId}/notifications?pageNumber=1&pageSize=20");
        Assert.Equal(HttpStatusCode.Forbidden, crossWorkspaceResponse.StatusCode);

        await alphaOwnerClient.CreateMembershipAsync(alpha.WorkspaceId, "beta-owner@example.com", "Member");
        await betaOwnerClient.LoginAsync("beta-owner@example.com", "Password123!");

        var memberResponse = await betaOwnerClient.GetAsync($"/workspaces/{alpha.WorkspaceId}/notification-preferences");
        Assert.Equal(HttpStatusCode.Forbidden, memberResponse.StatusCode);
    }
}
