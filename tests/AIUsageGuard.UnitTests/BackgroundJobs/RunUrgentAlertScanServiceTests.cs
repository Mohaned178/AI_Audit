using AIUsageGuard.Application.BackgroundJobs.RunUrgentAlertScan;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Notifications;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AIUsageGuard.UnitTests.BackgroundJobs;

public sealed class RunUrgentAlertScanServiceTests
{
    [Fact]
    public async Task Run_creates_one_notification_and_suppresses_duplicates()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var setup = await SeedWorkspaceAsync(dbContext);
        dbContext.NotificationPreferences.Add(new NotificationPreference
        {
            WorkspaceId = setup.WorkspaceId,
            UrgentAlertsEnabled = true,
            LastUpdatedByUserId = setup.UserId
        });
        var eventId = Guid.NewGuid();
        dbContext.AIUsageEvents.Add(new AIUsageEvent
        {
            Id = eventId,
            WorkspaceId = setup.WorkspaceId,
            ActorUserId = setup.UserId,
            EventType = AIUsageEventType.PromptSubmitted,
            IdempotencyKey = "urgent-evt-001",
            ToolName = "ChatGPT"
        });
        var outcomeId = Guid.NewGuid();
        dbContext.RiskEvaluationOutcomes.Add(new RiskEvaluationOutcome
        {
            Id = outcomeId,
            WorkspaceId = setup.WorkspaceId,
            EventId = eventId,
            AppliedRuleVersion = "v1",
            MatchedRuleCount = 1,
            EvaluationResult = RiskEvaluationResult.Matched
        });
        dbContext.RiskFindings.Add(new RiskFinding
        {
            WorkspaceId = setup.WorkspaceId,
            EventId = eventId,
            EvaluationOutcomeId = outcomeId,
            ActorUserId = setup.UserId,
            ToolName = "ChatGPT",
            Severity = RiskSeverity.High,
            Reason = "Sensitive data pattern detected."
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var first = await service.RunAsync(new RunUrgentAlertScanCommand(DateTimeOffset.UtcNow));
        var second = await service.RunAsync(new RunUrgentAlertScanCommand(DateTimeOffset.UtcNow.AddMinutes(1)));

        Assert.Equal(1, first.NotificationsCreated);
        Assert.Equal(0, second.NotificationsCreated);
        Assert.Equal(1, second.NotificationsSkipped);
        Assert.Equal(1, await dbContext.Notifications.CountAsync());
        Assert.Equal(NotificationStatus.Pending, (await dbContext.Notifications.SingleAsync()).Status);
    }

    private static RunUrgentAlertScanService CreateService(IPlatformStore store)
    {
        return new RunUrgentAlertScanService(
            store,
            new AuditService(store),
            Options.Create(new NotificationProcessingOptions()),
            NullLogger<RunUrgentAlertScanService>.Instance);
    }

    private static async Task<(Guid WorkspaceId, Guid UserId)> SeedWorkspaceAsync(ApplicationDbContext dbContext)
    {
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await dbContext.AddUserAsync(new UserAccount
        {
            Id = userId,
            Email = "owner@example.com",
            DisplayName = "Owner",
            PasswordHash = "hash",
            Status = UserAccountStatus.Active
        });
        dbContext.Workspaces.Add(new Workspace
        {
            Id = workspaceId,
            Name = "Alpha Workspace",
            Slug = "alpha-workspace",
            CreatedByUserId = userId
        });
        dbContext.WorkspaceMemberships.Add(new WorkspaceMembership
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = WorkspaceRole.Owner,
            Status = MembershipStatus.Active
        });
        await dbContext.SaveChangesAsync();
        return (workspaceId, userId);
    }
}
