using AIUsageGuard.Api.Contracts.Notifications;
using AIUsageGuard.Application.BackgroundJobs.RunUrgentAlertScan;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Notifications.DeliverPendingNotifications;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Notifications;

public sealed class UrgentAlertNotificationTests
{
    [Fact]
    public async Task Urgent_alert_scan_creates_history_and_delivers_to_workspace_owner()
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
                IdempotencyKey = "urgent-int-001",
                ToolName = "ChatGPT"
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
                Reason = "Sensitive data pattern detected."
            });

            return dbContext.SaveChangesAsync();
        });

        await factory.ExecuteServiceAsync<RunUrgentAlertScanService>(service =>
            service.RunAsync(new RunUrgentAlertScanCommand(DateTimeOffset.UtcNow)));
        await factory.ExecuteServiceAsync<DeliverPendingNotificationsService>(service =>
            service.RunAsync(new DeliverPendingNotificationsCommand(DateTimeOffset.UtcNow)));

        var sink = await factory.GetNotificationTestSinkAsync();
        Assert.Single(sink.Messages);

        var list = await client.ListNotificationsAsync(session.WorkspaceId, type: "urgent_alert");
        Assert.Single(list.Items);

        var detail = await client.GetNotificationAsync(session.WorkspaceId, list.Items[0].Id);
        Assert.Equal("urgent_alert", detail.Type);
        Assert.Single(detail.Deliveries);
        Assert.Equal("owner@example.com", detail.Deliveries[0].RecipientAddress);
    }
}
