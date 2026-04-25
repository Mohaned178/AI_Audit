using AIUsageGuard.Api.Contracts.Notifications;
using AIUsageGuard.Application.Models;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Notifications;

public sealed class NotificationContractsTests
{
    [Fact]
    public async Task Notification_preference_and_history_endpoints_return_stable_contract_shapes()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();
        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var updated = await client.UpdateNotificationPreferencesAsync(
            session.WorkspaceId,
            new UpdateNotificationPreferenceRequest(true, true, "daily", "all_admins_and_owners", []));

        await factory.ExecuteDbContextAsync(dbContext =>
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
                Status = NotificationStatus.Delivered
            };
            dbContext.Notifications.Add(notification);
            dbContext.NotificationDeliveryOutcomes.Add(new NotificationDeliveryOutcome
            {
                NotificationId = notification.Id,
                RecipientUserId = session.UserId,
                RecipientAddress = "owner@example.com",
                DeliveryStatus = DeliveryStatus.Delivered,
                AttemptCount = 1
            });

            return dbContext.SaveChangesAsync();
        });

        var preferences = await client.GetNotificationPreferencesAsync(session.WorkspaceId);
        var list = await client.ListNotificationsAsync(session.WorkspaceId);
        var detail = await client.GetNotificationAsync(session.WorkspaceId, list.Items.Single().Id);

        Assert.True(updated.UrgentAlertsEnabled);
        Assert.Equal("daily", preferences.DigestCadence);
        Assert.Equal("urgent_alert", list.Items.Single().Type);
        Assert.Equal("delivered", detail.Deliveries.Single().Status);
    }
}
