using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.Api.Contracts.RiskDetection;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.RiskDetection;

public sealed class RiskEvaluationOutcomeTests
{
    [Fact]
    public async Task Matching_event_records_all_supported_rule_hits_and_matched_outcome()
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
                "evt-match-001",
                "file_uploaded",
                DateTimeOffset.UtcNow,
                "Claude",
                null,
                null,
                "Contact owner@example.com",
                "draft.pdf",
                128,
                null,
                null,
                3m,
                null));

        var outcome = await factory.ExecuteDbContextAsync(db => db.RiskEvaluationOutcomes.SingleAsync());

        Assert.Equal("Matched", outcome.EvaluationResult.ToString());
        Assert.Equal(4, outcome.MatchedRuleCount);
        Assert.Equal(4, await factory.ExecuteDbContextAsync(db => db.RiskFindings.CountAsync()));
    }

    [Fact]
    public async Task Clean_event_records_no_match_outcome_without_findings()
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
            new UpdateWorkspaceRiskPolicyRequest(["ChatGPT"], 5m, 20m));

        await client.IngestAIUsageEventAsync(
            session.WorkspaceId,
            new IngestAIUsageEventRequest(
                "evt-clean-001",
                "prompt_submitted",
                DateTimeOffset.UtcNow,
                "ChatGPT",
                "gpt-5.4",
                null,
                "Summarize the roadmap.",
                null,
                null,
                null,
                null,
                0.3m,
                null));

        var outcome = await factory.ExecuteDbContextAsync(db => db.RiskEvaluationOutcomes.SingleAsync());

        Assert.Equal("NoMatch", outcome.EvaluationResult.ToString());
        Assert.Equal(0, outcome.MatchedRuleCount);
        Assert.Equal(0, await factory.ExecuteDbContextAsync(db => db.RiskFindings.CountAsync()));
    }
}
