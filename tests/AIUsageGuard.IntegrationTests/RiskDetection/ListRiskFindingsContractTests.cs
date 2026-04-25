using System.Net;
using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.Api.Contracts.RiskDetection;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.RiskDetection;

public sealed class ListRiskFindingsContractTests
{
    [Fact]
    public async Task List_rejects_unsupported_rule_type_filter_with_problem_details()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var response = await client.ListRiskFindingsResponseAsync(
            session.WorkspaceId,
            new ListRiskFindingsRequest("bad_rule", null, null, null, null, null, null, 1, 50));

        var problem = await response.ReadProblemAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task List_returns_string_enum_values_and_paging_fields()
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
                "evt-contract-001",
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

        var response = await client.ListRiskFindingsAsync(
            session.WorkspaceId,
            new ListRiskFindingsRequest(null, null, null, null, null, null, null, 1, 50));

        Assert.NotEmpty(response.Items);
        Assert.True(response.PageNumber >= 1);
        Assert.Contains(response.Items, item => item.RuleType == "sensitive_data_pattern");
    }
}
