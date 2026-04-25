using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Notifications;
using AIUsageGuard.Application.Notifications.DeliverPendingNotifications;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AIUsageGuard.UnitTests.Notifications;

public sealed class DeliverPendingNotificationsServiceTests
{
    [Fact]
    public async Task Run_delivers_pending_notification_to_eligible_admins()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var setup = await SeedWorkspaceAsync(dbContext);
        var notification = await SeedNotificationAsync(dbContext, setup.WorkspaceId, setup.UserId);
        var sender = new FakeNotificationSender();
        var service = CreateService(dbContext, sender);

        var result = await service.RunAsync(new DeliverPendingNotificationsCommand(DateTimeOffset.UtcNow));

        var refreshedNotification = await dbContext.Notifications.SingleAsync(item => item.Id == notification.Id);
        var outcome = await dbContext.NotificationDeliveryOutcomes.SingleAsync(item => item.NotificationId == notification.Id);

        Assert.Equal(1, result.DeliveredCount);
        Assert.Equal(NotificationStatus.Delivered, refreshedNotification.Status);
        Assert.Equal(DeliveryStatus.Delivered, outcome.DeliveryStatus);
        Assert.Single(sender.Messages);
    }

    [Fact]
    public async Task Run_schedules_retry_when_sender_fails_before_max_attempts()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var setup = await SeedWorkspaceAsync(dbContext);
        var notification = await SeedNotificationAsync(dbContext, setup.WorkspaceId, setup.UserId);
        var sender = new FakeNotificationSender(failuresRemaining: 1);
        var service = CreateService(dbContext, sender);

        var result = await service.RunAsync(new DeliverPendingNotificationsCommand(DateTimeOffset.UtcNow));

        var outcome = await dbContext.NotificationDeliveryOutcomes.SingleAsync(item => item.NotificationId == notification.Id);
        Assert.Equal(1, result.RetryScheduledCount);
        Assert.Equal(DeliveryStatus.RetryScheduled, outcome.DeliveryStatus);
        Assert.NotNull(outcome.NextAttemptAtUtc);
    }

    private static DeliverPendingNotificationsService CreateService(IPlatformStore store, INotificationSender sender)
    {
        return new DeliverPendingNotificationsService(
            store,
            sender,
            new AuditService(store),
            Options.Create(new NotificationProcessingOptions()),
            NullLogger<DeliverPendingNotificationsService>.Instance);
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
        dbContext.NotificationPreferences.Add(new NotificationPreference
        {
            WorkspaceId = workspaceId,
            UrgentAlertsEnabled = true,
            RecipientSelectionMode = RecipientSelectionMode.AllAdminsAndOwners,
            LastUpdatedByUserId = userId
        });
        await dbContext.SaveChangesAsync();
        return (workspaceId, userId);
    }

    private static async Task<NotificationMessage> SeedNotificationAsync(ApplicationDbContext dbContext, Guid workspaceId, Guid userId)
    {
        var notification = new NotificationMessage
        {
            WorkspaceId = workspaceId,
            NotificationType = NotificationType.UrgentAlert,
            TriggerFingerprint = $"risk-finding:{Guid.NewGuid()}",
            Severity = RiskSeverity.High,
            Subject = "High-risk AI activity detected",
            SummaryBody = "A high-severity workspace finding requires review.",
            CreatedByJobRunId = Guid.NewGuid(),
            Status = NotificationStatus.Pending
        };
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();
        return notification;
    }

    private sealed class FakeNotificationSender : INotificationSender
    {
        private int _failuresRemaining;

        public FakeNotificationSender(int failuresRemaining = 0)
        {
            _failuresRemaining = failuresRemaining;
        }

        public List<(string RecipientAddress, string Subject)> Messages { get; } = [];

        public Task SendAsync(string recipientAddress, string subject, string summaryBody, CancellationToken cancellationToken = default)
        {
            if (_failuresRemaining > 0)
            {
                _failuresRemaining--;
                throw new InvalidOperationException("Planned delivery failure.");
            }

            Messages.Add((recipientAddress, subject));
            return Task.CompletedTask;
        }
    }
}
