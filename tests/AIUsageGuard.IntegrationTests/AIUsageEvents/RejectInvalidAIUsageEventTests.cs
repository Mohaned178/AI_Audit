using System.Net;
using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.AIUsageEvents;

public sealed class RejectInvalidAIUsageEventTests
{
    [Fact]
    public async Task Missing_required_fields_are_rejected()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var response = await client.IngestAIUsageEventResponseAsync(
            session.WorkspaceId,
            new IngestAIUsageEventRequest(
                string.Empty,
                "prompt_submitted",
                DateTimeOffset.UtcNow,
                string.Empty,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
