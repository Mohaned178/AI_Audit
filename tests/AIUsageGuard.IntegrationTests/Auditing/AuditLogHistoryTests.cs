using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Auditing;

public sealed class AuditLogHistoryTests
{
    [Fact]
    public async Task Owner_can_filter_and_open_workspace_audit_history()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var now = DateTimeOffset.UtcNow;
        var deniedAuditLogId = Guid.Parse("aaaaaaa1-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var successAuditLogId = Guid.Parse("aaaaaaa2-aaaa-aaaa-aaaa-aaaaaaaaaaa2");

        await SeedAuditLogAsync(factory, session.WorkspaceId, session.UserId, successAuditLogId, "billing.plan_status.read", "billing", "plan_status", "success", "Retrieved plan status.", now.AddMinutes(-10), false);
        await SeedAuditLogAsync(factory, session.WorkspaceId, session.UserId, deniedAuditLogId, "authorization.denied", "authorization", "audit_log_history", "denied", "User is not a member of the requested workspace.", now.AddMinutes(-5), true);

        var response = await client.ListAuditLogsAsync(session.WorkspaceId, result: "denied");
        var detail = await client.GetAuditLogAsync(session.WorkspaceId, deniedAuditLogId);

        Assert.Single(response.Page.Items);
        Assert.Equal("authorization.denied", response.Page.Items[0].ActionType);
        Assert.Equal("denied", response.Page.Items[0].Result);
        Assert.Equal(deniedAuditLogId, response.Page.Items[0].AuditLogId);
        Assert.Equal("authorization.denied", detail.AuditLog.ActionType);
        Assert.True(detail.AuditLog.IsSecurityRelevant);
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
        string reason,
        DateTimeOffset occurredAt,
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
                Reason = reason,
                Category = category,
                IsSecurityRelevant = isSecurityRelevant,
                OccurredAt = occurredAt
            });
            await dbContext.SaveChangesAsync();
        });
    }
}
