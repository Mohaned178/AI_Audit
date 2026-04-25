using System.Net;
using System.Net.Http.Json;
using AIUsageGuard.Api.Contracts.Auth;
using AIUsageGuard.Api.Contracts.RiskDetection;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Auditing;

public sealed class AuditHardeningRegressionTests
{
    [Fact]
    public async Task Hardening_failures_remain_auditable_and_standardized_across_workspaces()
    {
        await using var factory = new TestWebApplicationFactory();
        using var firstClient = await factory.CreateInitializedApiClientAsync();
        using var secondClient = await factory.CreateInitializedApiClientAsync();

        var firstSession = await firstClient.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        await firstClient.LogoutAsync();

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var response = await firstClient.PostAsJsonAsync(
                "/auth/login",
                new LoginRequest("owner@example.com", "WrongPassword!"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var problem = await response.ReadProblemAsync();
            Assert.Equal(401, problem.Status);
            Assert.Equal("Invalid credentials.", problem.Detail);
            Assert.True(problem.Extensions.ContainsKey("traceId"));
        }

        var lockoutResponse = await firstClient.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("owner@example.com", "WrongPassword!"));

        Assert.Equal((HttpStatusCode)423, lockoutResponse.StatusCode);
        var lockoutProblem = await lockoutResponse.ReadProblemAsync();
        Assert.Equal(423, lockoutProblem.Status);
        Assert.Equal("Account is temporarily locked. Try again later.", lockoutProblem.Detail);
        Assert.True(lockoutProblem.Extensions.ContainsKey("traceId"));

        var secondSession = await secondClient.RegisterWorkspaceAsync(
            "owner2@example.com",
            "Password123!",
            "Owner 2",
            "Beta Workspace");

        var protectedWriteResponse = await secondClient.UpdateRiskPolicyResponseAsync(
            secondSession.WorkspaceId,
            new UpdateWorkspaceRiskPolicyRequest(["ChatGPT"], 1m, 10m));

        Assert.Equal(HttpStatusCode.BadRequest, protectedWriteResponse.StatusCode);
        var protectedWriteProblem = await protectedWriteResponse.ReadProblemAsync();
        Assert.Equal(400, protectedWriteProblem.Status);
        Assert.Equal("Protected request integrity token is required.", protectedWriteProblem.Detail);
        Assert.True(protectedWriteProblem.Extensions.ContainsKey("traceId"));

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            Assert.Equal(
                5,
                await dbContext.AuditRecords.CountAsync(item =>
                    item.WorkspaceId == firstSession.WorkspaceId &&
                    item.IsSecurityRelevant &&
                    item.ActionType == "auth.login" &&
                    item.Result == "failed"));

            Assert.Equal(
                1,
                await dbContext.AuditRecords.CountAsync(item =>
                    item.WorkspaceId == secondSession.WorkspaceId &&
                    item.IsSecurityRelevant &&
                    item.ActionType == "risk_policy.update" &&
                    item.Result == "denied"));
        });
    }
}
