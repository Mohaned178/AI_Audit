using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.AIUsageEvents;

public sealed class AIUsageEventsRegressionTests
{
    [Fact]
    public async Task Ingest_then_list_round_trip_remains_workspace_scoped()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        await client.IngestAIUsageEventAsync(session.WorkspaceId, new IngestAIUsageEventRequest(
            "evt-regression",
            "usage_recorded",
            DateTimeOffset.UtcNow,
            "ChatGPT",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null));

        var response = await client.ListAIUsageEventsAsync(session.WorkspaceId, new ListAIUsageEventsRequest(null, null, null, null, null, 1, 50));

        Assert.Single(response.Items);
        Assert.Equal(session.WorkspaceId, response.Items[0].WorkspaceId);
        Assert.Equal(1, await factory.ExecuteDbContextAsync(dbContext => dbContext.AIUsageEvents.CountAsync()));
    }
}
