using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.AIUsageEvents;

public sealed class IngestAIUsageEventMemberAccessTests
{
    [Fact]
    public async Task Active_workspace_member_can_ingest_into_their_workspace()
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

        var response = await memberClient.IngestAIUsageEventAsync(
            ownerSession.WorkspaceId,
            new IngestAIUsageEventRequest(
                "evt-member",
                "tool_used",
                DateTimeOffset.UtcNow,
                "Claude",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null));

        Assert.Equal("accepted", response.Outcome);
        Assert.Equal(ownerSession.WorkspaceId, response.WorkspaceId);
    }
}
