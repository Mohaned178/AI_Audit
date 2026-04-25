using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.Api.Contracts.RiskDetection;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.RiskDetection;

public sealed class RiskDetectionRegressionTests
{
    [Fact]
    public async Task Policy_update_ingest_review_round_trip_remains_workspace_scoped()
    {
        await using var factory = new TestWebApplicationFactory();
        using var firstClient = await factory.CreateInitializedApiClientAsync();
        using var secondClient = await factory.CreateInitializedApiClientAsync();

        var first = await firstClient.RegisterWorkspaceAsync(
            "owner1@example.com",
            "Password123!",
            "Owner 1",
            "Alpha Workspace");

        var second = await secondClient.RegisterWorkspaceAsync(
            "owner2@example.com",
            "Password123!",
            "Owner 2",
            "Beta Workspace");

        await firstClient.UpdateRiskPolicyAsync(
            first.WorkspaceId,
            new UpdateWorkspaceRiskPolicyRequest(["ChatGPT"], 1m, 10m));

        await firstClient.IngestAIUsageEventAsync(
            first.WorkspaceId,
            new IngestAIUsageEventRequest(
                "evt-regression-risk",
                "prompt_submitted",
                DateTimeOffset.UtcNow,
                "Claude",
                null,
                null,
                "Email owner1@example.com",
                null,
                null,
                null,
                null,
                5m,
                null));

        var list = await firstClient.ListRiskFindingsAsync(
            first.WorkspaceId,
            new ListRiskFindingsRequest(null, null, null, null, null, null, null, 1, 50));

        var detail = await firstClient.GetRiskFindingAsync(first.WorkspaceId, list.Items[0].Id);
        var otherWorkspaceList = await secondClient.ListRiskFindingsAsync(
            second.WorkspaceId,
            new ListRiskFindingsRequest(null, null, null, null, null, null, null, 1, 50));

        Assert.NotEmpty(list.Items);
        Assert.Equal(first.WorkspaceId, detail.WorkspaceId);
        Assert.Empty(otherWorkspaceList.Items);
        Assert.Equal(1, await factory.ExecuteDbContextAsync(db => db.RiskEvaluationOutcomes.CountAsync(item => item.WorkspaceId == first.WorkspaceId)));
    }
}
