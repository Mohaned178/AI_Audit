using System.Net;
using System.Net.Http.Json;
using AIUsageGuard.Api.Contracts.AIUsageEvents;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Billing;

public sealed class BillingLimitEnforcementTests
{
    [Fact]
    public async Task Active_member_limit_blocks_membership_creation_beyond_hard_limit()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        var owner = await ownerClient.RegisterWorkspaceAsync("owner@example.com", "Password123!", "Owner", "Alpha Workspace");

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            for (var index = 0; index < 10; index++)
            {
                await dbContext.AddUserAsync(new AIUsageGuard.Application.Models.UserAccount
                {
                    Email = $"member{index}@example.com",
                    DisplayName = $"Member {index}",
                    PasswordHash = "hash"
                });
            }
        });

        for (var index = 0; index < 9; index++)
        {
            await ownerClient.CreateMembershipAsync(owner.WorkspaceId, $"member{index}@example.com", "Member");
        }

        var response = await ownerClient.CreateMembershipResponseAsync(owner.WorkspaceId, "member9@example.com", "Member");
        var planStatus = await ownerClient.GetPlanStatusAsync(owner.WorkspaceId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("restricted", planStatus.Metrics.Single(item => item.Dimension == "active_members").State);
        Assert.Equal(10m, planStatus.Metrics.Single(item => item.Dimension == "active_members").CurrentQuantity);
    }

    [Fact]
    public async Task Low_ai_activity_limit_transitions_from_warning_to_overage()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        var owner = await ownerClient.RegisterWorkspaceAsync("owner@example.com", "Password123!", "Owner", "Alpha Workspace");
        await BillingTestData.ConfigureLowAiActivityLimitAsync(
            factory,
            owner.WorkspaceId,
            includedQuantity: 1m,
            warningThresholdQuantity: 1m,
            AIUsageGuard.Application.Models.BillingLimitBehavior.AllowOverage);

        await ownerClient.IngestAIUsageEventAsync(owner.WorkspaceId, CreateEvent("evt-1"));
        var warningStatus = await ownerClient.GetPlanStatusAsync(owner.WorkspaceId);
        await ownerClient.IngestAIUsageEventAsync(owner.WorkspaceId, CreateEvent("evt-2"));
        var overageStatus = await ownerClient.GetPlanStatusAsync(owner.WorkspaceId);

        Assert.Equal("warning", warningStatus.Metrics.Single(item => item.Dimension == "ai_activity_events").State);
        Assert.Equal("overage", overageStatus.Metrics.Single(item => item.Dimension == "ai_activity_events").State);
        Assert.Equal(1m, overageStatus.Metrics.Single(item => item.Dimension == "ai_activity_events").OverageQuantity);
    }

    private static IngestAIUsageEventRequest CreateEvent(string idempotencyKey)
        => new(
            idempotencyKey,
            "tool_used",
            DateTimeOffset.UtcNow,
            "copilot",
            "gpt-5.4",
            "browser",
            null,
            null,
            null,
            10,
            5,
            1m,
            null);
}
