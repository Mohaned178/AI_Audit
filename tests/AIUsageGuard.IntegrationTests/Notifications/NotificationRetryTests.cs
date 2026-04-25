using AIUsageGuard.Api.Contracts.Notifications;
using AIUsageGuard.Application.BackgroundJobs.RunDeliveryRetry;
using AIUsageGuard.Application.BackgroundJobs.RunDigestGeneration;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Notifications.DeliverPendingNotifications;
using AIUsageGuard.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Notifications;

public sealed class NotificationRetryTests
{
    [Fact]
    public async Task Failed_delivery_is_retried_and_reaches_a_delivered_final_outcome()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();
        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        await client.UpdateNotificationPreferencesAsync(
            session.WorkspaceId,
            new UpdateNotificationPreferenceRequest(true, false, null, "all_admins_and_owners", []));

        Guid notificationId = Guid.Empty;
        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            var notification = new NotificationMessage
            {
                WorkspaceId = session.WorkspaceId,
                NotificationType = NotificationType.UrgentAlert,
                TriggerFingerprint = $"risk-finding:{Guid.NewGuid()}",
                Subject = "High-risk AI activity detected",
                SummaryBody = "A high-severity workspace finding requires review.",
                Severity = RiskSeverity.High,
                CreatedByJobRunId = Guid.NewGuid(),
                Status = NotificationStatus.Pending
            };
            dbContext.Notifications.Add(notification);
            await dbContext.SaveChangesAsync();
            notificationId = notification.Id;
        });

        var sink = await factory.GetNotificationTestSinkAsync();
        sink.PlanFailures("owner@example.com", 1);

        await factory.ExecuteServiceAsync<DeliverPendingNotificationsService>(service =>
            service.RunAsync(new DeliverPendingNotificationsCommand(DateTimeOffset.UtcNow)));

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            var outcome = await dbContext.NotificationDeliveryOutcomes.SingleAsync(item => item.NotificationId == notificationId);
            outcome.NextAttemptAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
            await dbContext.SaveChangesAsync();
        });

        await factory.ExecuteServiceAsync<RunDeliveryRetryService>(service =>
            service.RunAsync(new RunDeliveryRetryCommand(DateTimeOffset.UtcNow)));
        await factory.ExecuteServiceAsync<DeliverPendingNotificationsService>(service =>
            service.RunAsync(new DeliverPendingNotificationsCommand(DateTimeOffset.UtcNow.AddMinutes(1))));

        var detail = await client.GetNotificationAsync(session.WorkspaceId, notificationId);

        Assert.Single(detail.Deliveries);
        Assert.Equal("delivered", detail.Deliveries[0].Status);
        Assert.Equal(2, detail.Deliveries[0].AttemptCount);
    }

    [Fact]
    public async Task Empty_state_digest_is_still_generated_for_enabled_workspace()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();
        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        await client.UpdateNotificationPreferencesAsync(
            session.WorkspaceId,
            new UpdateNotificationPreferenceRequest(false, true, "daily", "all_admins_and_owners", []));

        await factory.ExecuteServiceAsync<RunDigestGenerationService>(service =>
            service.RunAsync(new RunDigestGenerationCommand(DateTimeOffset.UtcNow)));

        var list = await client.ListNotificationsAsync(session.WorkspaceId, type: "digest");
        var detail = await client.GetNotificationAsync(session.WorkspaceId, list.Items.Single().Id);

        Assert.Single(list.Items);
        Assert.Contains("Events: 0", detail.SummaryBody);
    }
}
