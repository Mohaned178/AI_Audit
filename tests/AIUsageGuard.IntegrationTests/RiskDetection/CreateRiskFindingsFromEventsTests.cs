using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.Api.Contracts.RiskDetection;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.RiskDetection;

public sealed class CreateRiskFindingsFromEventsTests
{
    [Fact]
    public async Task Accepted_event_creates_workspace_scoped_findings()
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

        var response = await client.IngestAIUsageEventAsync(
            session.WorkspaceId,
            new IngestAIUsageEventRequest(
                "evt-risk-001",
                "prompt_submitted",
                DateTimeOffset.UtcNow,
                "Claude",
                "claude-3-7-sonnet",
                null,
                "Send this update to owner@example.com",
                null,
                null,
                null,
                null,
                5m,
                null));

        Assert.Equal("accepted", response.Outcome);
        Assert.Equal(3, await factory.ExecuteDbContextAsync(db => db.RiskFindings.CountAsync()));
        Assert.Equal(1, await factory.ExecuteDbContextAsync(db => db.RiskEvaluationOutcomes.CountAsync()));
        Assert.True(await factory.ExecuteDbContextAsync(db => db.RiskFindings.AllAsync(item => item.WorkspaceId == session.WorkspaceId && item.ActorUserId == session.UserId)));
    }
}
