using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.AIUsageEvents;

public sealed class IngestAIUsageEventTests
{
    [Fact]
    public async Task Member_can_submit_and_persist_valid_ai_usage_event()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var response = await client.IngestAIUsageEventAsync(
            session.WorkspaceId,
            new IngestAIUsageEventRequest(
                "evt-001",
                "prompt_submitted",
                DateTimeOffset.UtcNow,
                "ChatGPT",
                "gpt-5.4",
                null,
                "hello",
                null,
                null,
                null,
                null,
                null,
                null));

        Assert.Equal("accepted", response.Outcome);
        Assert.Equal(session.WorkspaceId, response.WorkspaceId);
        Assert.Equal(1, await factory.ExecuteDbContextAsync(dbContext => dbContext.AIUsageEvents.CountAsync()));
    }
}
