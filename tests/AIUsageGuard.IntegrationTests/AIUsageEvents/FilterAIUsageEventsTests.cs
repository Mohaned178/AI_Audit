using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.AIUsageEvents;

public sealed class FilterAIUsageEventsTests
{
    [Fact]
    public async Task History_endpoint_supports_page_slicing_and_time_filters()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        await client.IngestAIUsageEventAsync(session.WorkspaceId, new IngestAIUsageEventRequest(
            "evt-001",
            "prompt_submitted",
            DateTimeOffset.UtcNow.AddMinutes(-20),
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

        await client.IngestAIUsageEventAsync(session.WorkspaceId, new IngestAIUsageEventRequest(
            "evt-002",
            "tool_used",
            DateTimeOffset.UtcNow.AddMinutes(-10),
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

        var response = await client.ListAIUsageEventsAsync(session.WorkspaceId, new ListAIUsageEventsRequest(
            null,
            null,
            null,
            DateTimeOffset.UtcNow.AddMinutes(-15),
            null,
            1,
            1));

        Assert.Single(response.Items);
        Assert.Equal("Claude", response.Items[0].ToolName);
        Assert.Equal(1, response.TotalCount);
    }
}
