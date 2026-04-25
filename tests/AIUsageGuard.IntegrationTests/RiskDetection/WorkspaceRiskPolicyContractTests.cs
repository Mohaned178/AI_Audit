using System.Net;
using AIUsageGuard.Api.Contracts.RiskDetection;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.RiskDetection;

public sealed class WorkspaceRiskPolicyContractTests
{
    [Fact]
    public async Task Update_rejects_negative_threshold_with_problem_details()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var response = await client.UpdateRiskPolicyResponseAsync(
            session.WorkspaceId,
            new UpdateWorkspaceRiskPolicyRequest(["ChatGPT"], -1m, null));

        var problem = await response.ReadProblemAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task Get_returns_workspace_policy_contract_fields()
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

        var response = await client.GetRiskPolicyAsync(session.WorkspaceId);

        Assert.Equal(session.WorkspaceId, response.WorkspaceId);
        Assert.Equal(["ChatGPT"], response.ApprovedTools);
        Assert.NotEqual(default, response.LastUpdatedAt);
        Assert.NotEqual(Guid.Empty, response.LastUpdatedByUserId);
    }
}
