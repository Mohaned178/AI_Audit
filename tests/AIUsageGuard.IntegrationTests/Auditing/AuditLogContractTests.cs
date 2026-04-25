using AIUsageGuard.Api.Contracts.AuditLogs;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Auditing;

public sealed class AuditLogContractTests
{
    [Fact]
    public async Task Audit_log_endpoints_match_expected_contract_shape()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var now = DateTimeOffset.UtcNow;
        var deniedAuditLogId = Guid.Parse("88888888-8888-8888-8888-888888888888");

        await SeedAuditLogAsync(factory, session.WorkspaceId, session.UserId, new AuditRecordSeed(
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            "billing.plan_status.read",
            "billing",
            "plan_status",
            "success",
            "Retrieved plan status for plan starter.",
            now.AddMinutes(-10),
            false,
            "corr-7f8af7d1",
            null,
            null));

        await SeedAuditLogAsync(factory, session.WorkspaceId, session.UserId, new AuditRecordSeed(
            deniedAuditLogId,
            "authorization.denied",
            "authorization",
            "billing_cycle_history",
            "denied",
            "User is not a member of the requested workspace.",
            now.AddMinutes(-5),
            true,
            "corr-98a1112c",
            "iphash-2ac4",
            "Mozilla/5.0"));

        var list = await client.ListAuditLogsAsync(session.WorkspaceId);
        var detail = await client.GetAuditLogAsync(session.WorkspaceId, deniedAuditLogId);

        Assert.Equal(session.WorkspaceId, list.WorkspaceId);
        Assert.Equal(2, list.Page.TotalCount);
        Assert.Equal("billing.plan_status.read", list.Page.Items[0].ActionType);
        Assert.Equal("authorization.denied", list.Page.Items[1].ActionType);
        Assert.Equal(session.WorkspaceId, detail.WorkspaceId);
        Assert.Equal(deniedAuditLogId, detail.AuditLog.AuditLogId);
        Assert.NotNull(detail.AuditLog.ClientContext);
        Assert.Equal("iphash-2ac4", detail.AuditLog.ClientContext!.IpAddressHash);
        Assert.Equal("Mozilla/5.0", detail.AuditLog.ClientContext.UserAgent);
    }

    private static async Task SeedAuditLogAsync(
        TestWebApplicationFactory factory,
        Guid workspaceId,
        Guid actorId,
        AuditRecordSeed seed)
    {
        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.AuditRecords.Add(new AIUsageGuard.Application.Models.AuditRecord
            {
                Id = seed.AuditLogId,
                WorkspaceId = workspaceId,
                ActorUserId = actorId,
                ActionType = seed.ActionType,
                TargetType = seed.TargetType,
                TargetId = workspaceId.ToString(),
                Result = seed.Result,
                Reason = seed.Reason,
                Category = seed.Category,
                IsSecurityRelevant = seed.IsSecurityRelevant,
                CorrelationId = seed.CorrelationId,
                ClientIpAddressHash = seed.ClientIpAddressHash,
                UserAgent = seed.UserAgent,
                OccurredAt = seed.OccurredAtUtc
            });
            await dbContext.SaveChangesAsync();
        });
    }

    private sealed record AuditRecordSeed(
        Guid AuditLogId,
        string ActionType,
        string Category,
        string TargetType,
        string Result,
        string Reason,
        DateTimeOffset OccurredAtUtc,
        bool IsSecurityRelevant,
        string? CorrelationId,
        string? ClientIpAddressHash,
        string? UserAgent);
}
