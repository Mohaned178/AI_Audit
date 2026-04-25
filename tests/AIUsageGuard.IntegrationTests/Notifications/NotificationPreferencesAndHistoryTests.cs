using AIUsageGuard.Api.Contracts.Notifications;
using AIUsageGuard.Application.Models;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Notifications;

public sealed class NotificationPreferencesAndHistoryTests
{
    [Fact]
    public async Task Admin_can_update_preferences_and_read_notification_history_for_their_workspace()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        using var adminClient = await factory.CreateInitializedApiClientAsync();

        var owner = await ownerClient.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");
        var admin = await adminClient.RegisterWorkspaceAsync(
            "admin@example.com",
            "Password123!",
            "Admin",
            "Admin Workspace");

        await ownerClient.CreateMembershipAsync(owner.WorkspaceId, "admin@example.com", "Admin");

        var updated = await ownerClient.UpdateNotificationPreferencesAsync(
            owner.WorkspaceId,
            new UpdateNotificationPreferenceRequest(true, true, "weekly", "selected_recipients", [admin.UserId]));

        Guid notificationId = Guid.Empty;
        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            var notification = new NotificationMessage
            {
                WorkspaceId = owner.WorkspaceId,
                NotificationType = NotificationType.Digest,
                TriggerFingerprint = $"digest:weekly:{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}",
                Subject = "Workspace activity digest",
                SummaryBody = "Events: 0. Findings: 0. Estimated cost: 0.",
                CreatedByJobRunId = Guid.NewGuid(),
                Status = NotificationStatus.Delivered
            };
            dbContext.Notifications.Add(notification);
            await dbContext.SaveChangesAsync();
            notificationId = notification.Id;

            dbContext.NotificationDeliveryOutcomes.Add(new NotificationDeliveryOutcome
            {
                NotificationId = notification.Id,
                RecipientUserId = admin.UserId,
                RecipientAddress = "admin@example.com",
                DeliveryStatus = DeliveryStatus.Delivered,
                AttemptCount = 1
            });
            await dbContext.SaveChangesAsync();
        });

        await adminClient.LoginAsync("admin@example.com", "Password123!");
        var preferences = await adminClient.GetNotificationPreferencesAsync(owner.WorkspaceId);
        var list = await adminClient.ListNotificationsAsync(owner.WorkspaceId);
        var detail = await adminClient.GetNotificationAsync(owner.WorkspaceId, notificationId);

        Assert.Equal("selected_recipients", updated.RecipientSelectionMode);
        Assert.Contains(admin.UserId, preferences.SelectedRecipientUserIds);
        Assert.Single(list.Items);
        Assert.Equal("admin@example.com", detail.Deliveries.Single().RecipientAddress);
    }
}
