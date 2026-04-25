using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.Api.Contracts.RiskDetection;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.RiskDetection;

public sealed class DeduplicateRiskFindingsTests
{
    [Fact]
    public async Task Duplicate_ingestion_does_not_create_duplicate_findings_or_outcomes()
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

        var request = new IngestAIUsageEventRequest(
            "evt-dup-001",
            "prompt_submitted",
            DateTimeOffset.UtcNow,
            "Claude",
            null,
            null,
            "Email owner@example.com",
            null,
            null,
            null,
            null,
            5m,
            null);

        var first = await client.IngestAIUsageEventAsync(session.WorkspaceId, request);
        var second = await client.IngestAIUsageEventAsync(session.WorkspaceId, request);

        Assert.Equal("accepted", first.Outcome);
        Assert.Equal("duplicate", second.Outcome);
        Assert.Equal(1, await factory.ExecuteDbContextAsync(db => db.RiskEvaluationOutcomes.CountAsync()));
        Assert.Equal(3, await factory.ExecuteDbContextAsync(db => db.RiskFindings.CountAsync()));
    }
}
