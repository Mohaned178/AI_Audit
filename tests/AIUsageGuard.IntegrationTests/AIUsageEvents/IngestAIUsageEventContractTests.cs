using System.Net;
using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.AIUsageEvents;

public sealed class IngestAIUsageEventContractTests
{
    [Fact]
    public async Task Ingest_rejects_unsupported_event_type_with_problem_details()
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
                "evt-bad",
                "unknown_type",
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

        var problem = await response.ReadProblemAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(400, problem.Status);
    }
}
