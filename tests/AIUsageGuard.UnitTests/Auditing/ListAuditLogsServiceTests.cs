using AIUsageGuard.Application.Auditing.ListAuditLogs;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.UnitTests.Auditing;

public sealed class ListAuditLogsServiceTests
{
    [Fact]
    public async Task List_returns_newest_entries_first_and_resolves_actor_names()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        await SeedWorkspaceAsync(dbContext, workspaceId, actorId);
        dbContext.AuditRecords.Add(new AuditRecord
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkspaceId = workspaceId,
            ActorUserId = actorId,
            ActionType = "billing.plan_status.read",
            TargetType = "plan_status",
            TargetId = workspaceId.ToString(),
            Result = "success",
            Reason = "Retrieved plan status.",
            Category = "billing",
            OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        });
        dbContext.AuditRecords.Add(new AuditRecord
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            WorkspaceId = workspaceId,
            ActorUserId = actorId,
            ActionType = "authorization.denied",
            TargetType = "audit_log_history",
            TargetId = workspaceId.ToString(),
            Result = "denied",
            Reason = "User is not a member of the requested workspace.",
            Category = "authorization",
            IsSecurityRelevant = true,
            OccurredAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new ListAuditLogsService(dbContext, new AuditService(dbContext));

        var result = await service.ListAsync(new ListAuditLogsQuery(
            workspaceId,
            actorId,
            null,
            null,
            null,
            null,
            null,
            null,
            1,
            20));

        Assert.Equal(workspaceId, result.Page.WorkspaceId);
        Assert.Equal(2, result.Page.TotalCount);
        Assert.Equal("authorization.denied", result.Page.Items[0].ActionType);
        Assert.Equal("Owner One", result.Page.Items[0].ActorDisplayName);
        Assert.Equal("billing.plan_status.read", result.Page.Items[1].ActionType);
        Assert.True(await dbContext.AuditRecords.AnyAsync(item => item.ActionType == "audit_log.list" && item.Result == "success"));
    }

    [Fact]
    public async Task List_rejects_unsupported_result_filter_and_records_failure()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        await SeedWorkspaceAsync(dbContext, workspaceId, actorId);
        var service = new ListAuditLogsService(dbContext, new AuditService(dbContext));

        var exception = await Assert.ThrowsAsync<RequestFailureException>(() =>
            service.ListAsync(new ListAuditLogsQuery(
                workspaceId,
                actorId,
                null,
                null,
                null,
                "bad",
                null,
                null,
                1,
                20)));

        Assert.Equal(400, exception.StatusCode);
        Assert.True(await dbContext.AuditRecords.AnyAsync(item => item.ActionType == "audit_log.list" && item.Result == "failed"));
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
