using System.Net;
using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.AIUsageEvents;

public sealed class ListAIUsageEventsAuthorizationTests
{
    [Fact]
    public async Task Non_admin_member_cannot_list_event_history()
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

        var response = await memberClient.ListAIUsageEventsResponseAsync(
            ownerSession.WorkspaceId,
            new ListAIUsageEventsRequest(null, null, null, null, null, 1, 50));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
