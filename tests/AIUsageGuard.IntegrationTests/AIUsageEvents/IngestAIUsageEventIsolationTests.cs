using System.Net;
using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.AIUsageEvents;

public sealed class IngestAIUsageEventIsolationTests
{
    [Fact]
    public async Task User_cannot_ingest_into_workspace_they_do_not_belong_to()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        using var otherClient = await factory.CreateInitializedApiClientAsync();

        var ownerSession = await ownerClient.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var otherSession = await otherClient.RegisterWorkspaceAsync(
            "other@example.com",
            "Password123!",
            "Other",
            "Beta Workspace");

        var response = await otherClient.IngestAIUsageEventResponseAsync(
            ownerSession.WorkspaceId,
            new IngestAIUsageEventRequest(
                "evt-cross",
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

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(otherSession.WorkspaceId, otherSession.WorkspaceId);
    }
}
