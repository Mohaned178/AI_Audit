using System.Net;
using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.Api.Contracts.RiskDetection;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.RiskDetection;

public sealed class RiskFindingsAuthorizationTests
{
    [Fact]
    public async Task Non_admin_member_cannot_review_risk_findings()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        using var memberClient = await factory.CreateInitializedApiClientAsync();

        var ownerSession = await ownerClient.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        await memberClient.RegisterWorkspaceAsync(
            "member@example.com",
            "Password123!",
            "Member",
            "Beta Workspace");

        await ownerClient.CreateMembershipAsync(ownerSession.WorkspaceId, "member@example.com", "Member");

        var response = await memberClient.ListRiskFindingsResponseAsync(
            ownerSession.WorkspaceId,
            new ListRiskFindingsRequest(null, null, null, null, null, null, null, 1, 50));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_cannot_review_findings_for_another_workspace()
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

        var response = await secondClient.ListRiskFindingsResponseAsync(
            first.WorkspaceId,
            new ListRiskFindingsRequest(null, null, null, null, null, null, null, 1, 50));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_supports_workspace_scoped_rule_filters()
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
                "evt-filter-001",
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

        var filtered = await client.ListRiskFindingsAsync(
            session.WorkspaceId,
            new ListRiskFindingsRequest("sensitive_data_pattern", null, null, null, null, null, null, 1, 50));

        Assert.NotEmpty(filtered.Items);
        Assert.All(filtered.Items, item => Assert.Equal("sensitive_data_pattern", item.RuleType));
    }
}
