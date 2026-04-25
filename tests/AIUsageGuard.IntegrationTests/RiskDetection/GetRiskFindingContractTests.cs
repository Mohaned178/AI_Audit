using System.Net;
using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.Api.Contracts.RiskDetection;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.RiskDetection;

public sealed class GetRiskFindingContractTests
{
    [Fact]
    public async Task Get_returns_not_found_problem_details_for_missing_finding()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var response = await client.GetRiskFindingResponseAsync(session.WorkspaceId, Guid.NewGuid());
        var problem = await response.ReadProblemAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task Get_returns_triggering_event_context_for_existing_finding()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        await client.UpdateRiskPolicyAsync(
            session.WorkspaceId,
            new UpdateWorkspaceRiskPolicyRequest(["ChatGPT"], 1m, 10m));

        await client.IngestAIUsageEventAsync(
            session.WorkspaceId,
            new IngestAIUsageEventRequest(
                "evt-detail-001",
                "prompt_submitted",
                DateTimeOffset.UtcNow,
                "Claude",
                "claude-3-7-sonnet",
                null,
                "Email owner@example.com",
                null,
                null,
                null,
                null,
                5m,
                null));

        var list = await client.ListRiskFindingsAsync(
            session.WorkspaceId,
            new ListRiskFindingsRequest(null, null, null, null, null, null, null, 1, 50));

        var detail = await client.GetRiskFindingAsync(session.WorkspaceId, list.Items[0].Id);

        Assert.Equal(list.Items[0].Id, detail.Id);
        Assert.Equal("prompt_submitted", detail.Event.EventType);
        Assert.Equal(session.WorkspaceId, detail.WorkspaceId);
    }
}
