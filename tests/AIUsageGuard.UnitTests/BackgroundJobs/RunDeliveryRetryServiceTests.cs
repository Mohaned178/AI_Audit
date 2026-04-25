using AIUsageGuard.Application.BackgroundJobs.RunDeliveryRetry;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Notifications;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AIUsageGuard.UnitTests.BackgroundJobs;

public sealed class RunDeliveryRetryServiceTests
{
    [Fact]
    public async Task Run_requeues_due_retry_outcomes()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var notification = new NotificationMessage
        {
            WorkspaceId = workspaceId,
            NotificationType = NotificationType.UrgentAlert,
            TriggerFingerprint = $"risk-finding:{Guid.NewGuid()}",
            Subject = "High-risk AI activity detected",
            SummaryBody = "summary",
            CreatedByJobRunId = Guid.NewGuid(),
            Status = NotificationStatus.Pending
        };
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();
        dbContext.NotificationDeliveryOutcomes.Add(new NotificationDeliveryOutcome
        {
            NotificationId = notification.Id,
            RecipientUserId = Guid.NewGuid(),
            RecipientAddress = "owner@example.com",
            DeliveryStatus = DeliveryStatus.RetryScheduled,
            AttemptCount = 1,
            NextAttemptAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        await dbContext.SaveChangesAsync();

        var service = new RunDeliveryRetryService(
            dbContext,
            new AuditService(dbContext),
            Options.Create(new NotificationProcessingOptions()),
            NullLogger<RunDeliveryRetryService>.Instance);

        var result = await service.RunAsync(new RunDeliveryRetryCommand(DateTimeOffset.UtcNow));

        var outcome = await dbContext.NotificationDeliveryOutcomes.SingleAsync();
        Assert.Equal(1, result.RequeuedOutcomeCount);
        Assert.Equal(DeliveryStatus.Pending, outcome.DeliveryStatus);
        Assert.Null(outcome.NextAttemptAtUtc);
    }
}
