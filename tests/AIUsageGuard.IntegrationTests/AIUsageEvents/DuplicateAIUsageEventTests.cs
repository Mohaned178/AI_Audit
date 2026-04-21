using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.AIUsageEvents;

public sealed class DuplicateAIUsageEventTests
{
    [Fact]
    public async Task Duplicate_submission_returns_duplicate_outcome_without_second_record()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var request = new IngestAIUsageEventRequest(
            "evt-dup",
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
            null);

        var first = await client.IngestAIUsageEventAsync(session.WorkspaceId, request);
        var second = await client.IngestAIUsageEventAsync(session.WorkspaceId, request);

        Assert.Equal("accepted", first.Outcome);
        Assert.Equal("duplicate", second.Outcome);
        Assert.Equal(1, await factory.ExecuteDbContextAsync(dbContext => dbContext.AIUsageEvents.CountAsync()));
    }
}
