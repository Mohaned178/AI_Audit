using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.Api.Contracts.RiskDetection;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.RiskDetection;

public sealed class WorkspaceRiskPolicyIntegrationTests
{
    [Fact]
    public async Task Updated_policy_changes_future_risk_evaluation_results()
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
                "evt-policy-before",
                "prompt_submitted",
                DateTimeOffset.UtcNow.AddMinutes(-1),
                "Claude",
                null,
                null,
                "Summarize the roadmap.",
                null,
                null,
                null,
                null,
                5m,
                null));

        var findingsBefore = await factory.ExecuteDbContextAsync(db => db.RiskFindings.CountAsync());

        await client.UpdateRiskPolicyAsync(
            session.WorkspaceId,
            new UpdateWorkspaceRiskPolicyRequest(["ChatGPT", "Claude"], 10m, 100m));

        await client.IngestAIUsageEventAsync(
            session.WorkspaceId,
            new IngestAIUsageEventRequest(
                "evt-policy-after",
                "prompt_submitted",
                DateTimeOffset.UtcNow,
                "Claude",
                null,
                null,
                "Summarize the roadmap.",
                null,
                null,
                null,
                null,
                1m,
                null));

        var findingsAfter = await factory.ExecuteDbContextAsync(db => db.RiskFindings.CountAsync());
        var outcomes = (await factory.ExecuteDbContextAsync(db => db.RiskEvaluationOutcomes.ToListAsync()))
            .OrderBy(item => item.EvaluatedAt)
            .ToList();

        Assert.True(findingsBefore > 0);
        Assert.Equal(findingsBefore, findingsAfter);
        Assert.Equal("Matched", outcomes[0].EvaluationResult.ToString());
        Assert.Equal("NoMatch", outcomes[1].EvaluationResult.ToString());
    }
}
