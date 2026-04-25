using System.Net;
using AIUsageGuard.Api.Contracts.RiskDetection;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.RiskDetection;

public sealed class WorkspaceRiskPolicyAuthorizationTests
{
    [Fact]
    public async Task Non_admin_member_cannot_read_or_update_workspace_policy()
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

        var getResponse = await memberClient.GetRiskPolicyResponseAsync(ownerSession.WorkspaceId);
        var updateResponse = await memberClient.UpdateRiskPolicyResponseAsync(
            ownerSession.WorkspaceId,
            new UpdateWorkspaceRiskPolicyRequest(["ChatGPT"], 1m, 10m));

        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task Admin_cannot_read_or_update_another_workspaces_policy()
    {
        await using var factory = new TestWebApplicationFactory();
        using var firstClient = await factory.CreateInitializedApiClientAsync();
        using var secondClient = await factory.CreateInitializedApiClientAsync();

        var first = await firstClient.RegisterWorkspaceAsync(
            "owner1@example.com",
            "Password123!",
            "Owner 1",
            "Alpha Workspace");

        await secondClient.RegisterWorkspaceAsync(
            "owner2@example.com",
            "Password123!",
            "Owner 2",
            "Beta Workspace");

        var getResponse = await secondClient.GetRiskPolicyResponseAsync(first.WorkspaceId);
        var updateResponse = await secondClient.UpdateRiskPolicyResponseAsync(
            first.WorkspaceId,
            new UpdateWorkspaceRiskPolicyRequest(["ChatGPT"], 1m, 10m));

        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }
}
