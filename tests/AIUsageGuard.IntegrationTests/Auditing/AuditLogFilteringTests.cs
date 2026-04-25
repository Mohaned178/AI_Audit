using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Auditing;

public sealed class AuditLogFilteringTests
{
    [Fact]
    public async Task List_filters_by_action_result_actor_and_security_relevance()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var actorId = session.UserId;
        var otherActorId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        await SeedAuditLogAsync(factory, session.WorkspaceId, actorId, Guid.Parse("bbbbbbb1-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), "authorization.denied", "authorization", "audit_log_history", "denied", true);
        await SeedAuditLogAsync(factory, session.WorkspaceId, actorId, Guid.Parse("bbbbbbb2-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), "authorization.denied", "authorization", "audit_log_history", "success", false);
        await SeedAuditLogAsync(factory, session.WorkspaceId, otherActorId, Guid.Parse("bbbbbbb3-bbbb-bbbb-bbbb-bbbbbbbbbbb3"), "billing.plan_status.read", "billing", "plan_status", "success", false);

        var response = await client.ListAuditLogsAsync(
            session.WorkspaceId,
            actionType: "authorization.denied",
            result: "denied",
            actorUserId: actorId,
            securityRelevant: true);

        Assert.Single(response.Page.Items);
        Assert.Equal("authorization.denied", response.Page.Items[0].ActionType);
        Assert.Equal("denied", response.Page.Items[0].Result);
        Assert.True(response.Page.Items[0].IsSecurityRelevant);
    }

    private static async Task SeedAuditLogAsync(
        TestWebApplicationFactory factory,
        Guid workspaceId,
        Guid actorId,
        Guid auditLogId,
        string actionType,
        string category,
        string targetType,
        string result,
        bool isSecurityRelevant)
    {
        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.AuditRecords.Add(new AIUsageGuard.Application.Models.AuditRecord
            {
                Id = auditLogId,
                WorkspaceId = workspaceId,
                ActorUserId = actorId,
                ActionType = actionType,
                TargetType = targetType,
                TargetId = workspaceId.ToString(),
                Result = result,
                Reason = result == "denied" ? "User is not a member of the requested workspace." : "Retrieved plan status.",
                Category = category,
                IsSecurityRelevant = isSecurityRelevant,
                OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-1)
            });
            await dbContext.SaveChangesAsync();
        });
    }
}
