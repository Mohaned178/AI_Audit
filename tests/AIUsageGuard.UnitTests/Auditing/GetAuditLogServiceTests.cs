using AIUsageGuard.Application.Auditing.GetAuditLog;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.UnitTests.Auditing;

public sealed class GetAuditLogServiceTests
{
    [Fact]
    public async Task Get_returns_detail_with_client_context_and_records_audit()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var auditLogId = Guid.NewGuid();

        await SeedWorkspaceAsync(dbContext, workspaceId, actorId);
        dbContext.AuditRecords.Add(new AuditRecord
        {
            Id = auditLogId,
            WorkspaceId = workspaceId,
            ActorUserId = actorId,
            ActionType = "authorization.denied",
            TargetType = "audit_log_history",
            TargetId = workspaceId.ToString(),
            Result = "denied",
            Reason = "User is not a member of the requested workspace.",
            Category = "authorization",
            IsSecurityRelevant = true,
            CorrelationId = "corr-123",
            ClientIpAddressHash = "iphash-abc",
            UserAgent = "Mozilla/5.0",
            OccurredAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new GetAuditLogService(dbContext, new AuditService(dbContext));

        var result = await service.GetAsync(new GetAuditLogQuery(workspaceId, actorId, auditLogId));

        Assert.Equal(workspaceId, result.WorkspaceId);
        Assert.Equal(auditLogId, result.AuditLog.AuditLogId);
        Assert.Equal("Owner One", result.AuditLog.ActorDisplayName);
        Assert.NotNull(result.AuditLog.ClientIpAddressHash);
        Assert.Equal("iphash-abc", result.AuditLog.ClientIpAddressHash);
        Assert.Equal("Mozilla/5.0", result.AuditLog.UserAgent);
        Assert.True(await dbContext.AuditRecords.AnyAsync(item => item.ActionType == "audit_log.read" && item.Result == "success"));
    }

    [Fact]
    public async Task Get_rejects_missing_audit_log_and_records_failure()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        await SeedWorkspaceAsync(dbContext, workspaceId, actorId);
        var service = new GetAuditLogService(dbContext, new AuditService(dbContext));

        var auditLogId = Guid.NewGuid();
        var exception = await Assert.ThrowsAsync<RequestFailureException>(() =>
            service.GetAsync(new GetAuditLogQuery(workspaceId, actorId, auditLogId)));

        Assert.Equal(404, exception.StatusCode);
        Assert.True(await dbContext.AuditRecords.AnyAsync(item => item.ActionType == "audit_log.read" && item.Result == "failed"));
    }

    private static async Task SeedWorkspaceAsync(ApplicationDbContext dbContext, Guid workspaceId, Guid actorId)
    {
        await dbContext.AddUserAsync(new UserAccount
        {
            Id = actorId,
            Email = "owner@example.com",
            DisplayName = "Owner One",
            PasswordHash = "hash"
        });

        await dbContext.AddWorkspaceAsync(new Workspace
        {
            Id = workspaceId,
            Name = "Alpha Workspace",
            Slug = "alpha-workspace",
            CreatedByUserId = actorId
        });
    }
}
