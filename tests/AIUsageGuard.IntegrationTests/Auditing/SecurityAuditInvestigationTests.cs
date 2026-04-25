using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Auditing;

public sealed class SecurityAuditInvestigationTests
{
    [Fact]
    public async Task Owner_can_investigate_denied_security_relevant_audit_history()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var auditLogId = Guid.Parse("ccccccc1-cccc-cccc-cccc-ccccccccccc1");
        await SeedAuditLogAsync(factory, session.WorkspaceId, session.UserId, auditLogId);

        var response = await client.ListAuditLogsAsync(
            session.WorkspaceId,
            result: "denied",
            securityRelevant: true);

        Assert.Single(response.Page.Items);
        Assert.Equal("authorization.denied", response.Page.Items[0].ActionType);
        Assert.True(response.Page.Items[0].IsSecurityRelevant);

        var detail = await client.GetAuditLogAsync(session.WorkspaceId, auditLogId);
        Assert.NotNull(detail.AuditLog.ClientContext);
        Assert.Equal("authorization.denied", detail.AuditLog.ActionType);
        Assert.Equal("iphash-2ac4", detail.AuditLog.ClientContext!.IpAddressHash);
        Assert.Equal("Mozilla/5.0", detail.AuditLog.ClientContext.UserAgent);
    }

    private static async Task SeedAuditLogAsync(TestWebApplicationFactory factory, Guid workspaceId, Guid actorId, Guid auditLogId)
    {
        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.AuditRecords.Add(new AIUsageGuard.Application.Models.AuditRecord
            {
                Id = auditLogId,
                WorkspaceId = workspaceId,
                ActorUserId = actorId,
                ActionType = "authorization.denied",
                TargetType = "billing_cycle_history",
                TargetId = workspaceId.ToString(),
                Result = "denied",
                Reason = "User is not a member of the requested workspace.",
                Category = "authorization",
                IsSecurityRelevant = true,
                CorrelationId = "corr-98a1112c",
                ClientIpAddressHash = "iphash-2ac4",
                UserAgent = "Mozilla/5.0",
                OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-5)
            });
            await dbContext.SaveChangesAsync();
        });
    }
}
