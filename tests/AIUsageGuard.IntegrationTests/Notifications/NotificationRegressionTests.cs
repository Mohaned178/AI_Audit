using AIUsageGuard.Api.Contracts.Notifications;
using AIUsageGuard.Application.BackgroundJobs.RunDigestGeneration;
using AIUsageGuard.Application.BackgroundJobs.RunUrgentAlertScan;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Notifications.DeliverPendingNotifications;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Notifications;

public sealed class NotificationRegressionTests
{
    [Fact]
    public async Task Urgent_and_digest_notifications_coexist_and_history_filters_remain_stable()
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
            new UpdateNotificationPreferenceRequest(true, true, "daily", "all_admins_and_owners", []));

        await factory.ExecuteDbContextAsync(dbContext =>
        {
            var eventId = Guid.NewGuid();
            var outcomeId = Guid.NewGuid();
            dbContext.AIUsageEvents.Add(new AIUsageEvent
            {
                Id = eventId,
                WorkspaceId = session.WorkspaceId,
                ActorUserId = session.UserId,
                EventType = AIUsageEventType.PromptSubmitted,
                IdempotencyKey = "regression-evt-001",
                ToolName = "ChatGPT",
                OccurredAt = DateTimeOffset.UtcNow.AddDays(-1).AddHours(-2)
            });
            dbContext.RiskEvaluationOutcomes.Add(new RiskEvaluationOutcome
            {
                Id = outcomeId,
                WorkspaceId = session.WorkspaceId,
                EventId = eventId,
                AppliedRuleVersion = "v1",
                MatchedRuleCount = 1,
                EvaluationResult = RiskEvaluationResult.Matched
            });
            dbContext.RiskFindings.Add(new RiskFinding
            {
                WorkspaceId = session.WorkspaceId,
                EventId = eventId,
                EvaluationOutcomeId = outcomeId,
                ActorUserId = session.UserId,
                ToolName = "ChatGPT",
                Severity = RiskSeverity.High,
                Reason = "High-risk activity"
            });

            return dbContext.SaveChangesAsync();
        });

        await factory.ExecuteServiceAsync<RunUrgentAlertScanService>(service =>
            service.RunAsync(new RunUrgentAlertScanCommand(DateTimeOffset.UtcNow)));
        await factory.ExecuteServiceAsync<RunDigestGenerationService>(service =>
            service.RunAsync(new RunDigestGenerationCommand(DateTimeOffset.UtcNow)));
        await factory.ExecuteServiceAsync<DeliverPendingNotificationsService>(service =>
            service.RunAsync(new DeliverPendingNotificationsCommand(DateTimeOffset.UtcNow)));

        var allNotifications = await client.ListNotificationsAsync(session.WorkspaceId);
        var urgentNotifications = await client.ListNotificationsAsync(session.WorkspaceId, type: "urgent_alert");
        var digestNotifications = await client.ListNotificationsAsync(session.WorkspaceId, type: "digest");

        Assert.Equal(2, allNotifications.Items.Count);
        Assert.Single(urgentNotifications.Items);
        Assert.Single(digestNotifications.Items);
    }
}
