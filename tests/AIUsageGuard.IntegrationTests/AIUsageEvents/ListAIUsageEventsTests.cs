using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.AIUsageEvents;

public sealed class ListAIUsageEventsTests
{
    [Fact]
    public async Task Owner_can_list_workspace_events_with_basic_filtering()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        await client.IngestAIUsageEventAsync(
            session.WorkspaceId,
            new IngestAIUsageEventRequest(
                "evt-001",
                "prompt_submitted",
                DateTimeOffset.UtcNow.AddMinutes(-10),
                "ChatGPT",
                null,
                null,
                "hello",
                null,
                null,
                null,
                null,
                null,
                null));

        await client.IngestAIUsageEventAsync(
            session.WorkspaceId,
            new IngestAIUsageEventRequest(
                "evt-002",
                "tool_used",
                DateTimeOffset.UtcNow.AddMinutes(-5),
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

        var response = await client.ListAIUsageEventsAsync(
            session.WorkspaceId,
            new ListAIUsageEventsRequest("tool_used", null, null, null, null, 1, 50));

        Assert.Equal(1, response.TotalCount);
        Assert.Single(response.Items);
        Assert.Equal("Claude", response.Items[0].ToolName);
    }
}
