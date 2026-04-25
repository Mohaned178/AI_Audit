using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.Api.Contracts.RiskDetection;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.RiskDetection;

public sealed class ReviewRiskFindingsTests
{
    [Fact]
    public async Task Admin_can_list_and_open_risk_findings_for_workspace()
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
                "evt-review-001",
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
                null));

        var list = await client.ListRiskFindingsAsync(
            session.WorkspaceId,
            new ListRiskFindingsRequest(null, null, null, null, null, null, null, 1, 50));

        var detail = await client.GetRiskFindingAsync(session.WorkspaceId, list.Items[0].Id);

        Assert.NotEmpty(list.Items);
        Assert.Equal(list.Items[0].EventId, detail.EventId);
        Assert.False(string.IsNullOrWhiteSpace(detail.Reason));
        Assert.False(string.IsNullOrWhiteSpace(detail.AppliedRuleVersion));
    }
}
