using AIUsageGuard.Api.Contracts.Notifications;
using AIUsageGuard.Application.BackgroundJobs.RunUrgentAlertScan;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Notifications.DeliverPendingNotifications;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Notifications;

public sealed class UrgentAlertIsolationTests
{
    [Fact]
    public async Task Urgent_alerts_stay_workspace_scoped_and_duplicate_scans_do_not_repeat_notifications()
    {
        await using var factory = new TestWebApplicationFactory();
        using var alphaClient = await factory.CreateInitializedApiClientAsync();
        using var betaClient = await factory.CreateInitializedApiClientAsync();

        var alpha = await alphaClient.RegisterWorkspaceAsync(
            "alpha-owner@example.com",
            "Password123!",
            "Alpha Owner",
            "Alpha Workspace");
        var beta = await betaClient.RegisterWorkspaceAsync(
            "beta-owner@example.com",
            "Password123!",
            "Beta Owner",
            "Beta Workspace");

        await alphaClient.UpdateNotificationPreferencesAsync(
            alpha.WorkspaceId,
            new UpdateNotificationPreferenceRequest(true, false, null, "all_admins_and_owners", []));
        await betaClient.UpdateNotificationPreferencesAsync(
            beta.WorkspaceId,
            new UpdateNotificationPreferenceRequest(true, false, null, "all_admins_and_owners", []));

        await factory.ExecuteDbContextAsync(dbContext =>
        {
            var alphaEventId = Guid.NewGuid();
            var alphaOutcomeId = Guid.NewGuid();
            var betaEventId = Guid.NewGuid();
            var betaOutcomeId = Guid.NewGuid();

            dbContext.AIUsageEvents.AddRange(
                new AIUsageEvent
                {
                    Id = alphaEventId,
                    WorkspaceId = alpha.WorkspaceId,
                    ActorUserId = alpha.UserId,
                    EventType = AIUsageEventType.PromptSubmitted,
                    IdempotencyKey = "alpha-urgent-001",
                    ToolName = "ChatGPT"
                },
                new AIUsageEvent
                {
                    Id = betaEventId,
                    WorkspaceId = beta.WorkspaceId,
                    ActorUserId = beta.UserId,
                    EventType = AIUsageEventType.PromptSubmitted,
                    IdempotencyKey = "beta-urgent-001",
                    ToolName = "Claude"
                });
            dbContext.RiskEvaluationOutcomes.AddRange(
                new RiskEvaluationOutcome
                {
                    Id = alphaOutcomeId,
                    WorkspaceId = alpha.WorkspaceId,
                    EventId = alphaEventId,
                    AppliedRuleVersion = "v1",
                    MatchedRuleCount = 1,
                    EvaluationResult = RiskEvaluationResult.Matched
                },
                new RiskEvaluationOutcome
                {
                    Id = betaOutcomeId,
                    WorkspaceId = beta.WorkspaceId,
                    EventId = betaEventId,
                    AppliedRuleVersion = "v1",
                    MatchedRuleCount = 1,
                    EvaluationResult = RiskEvaluationResult.Matched
                });
            dbContext.RiskFindings.AddRange(
                new RiskFinding
                {
                    WorkspaceId = alpha.WorkspaceId,
                    EventId = alphaEventId,
                    EvaluationOutcomeId = alphaOutcomeId,
                    ActorUserId = alpha.UserId,
                    ToolName = "ChatGPT",
                    Severity = RiskSeverity.High,
                    Reason = "Alpha finding"
                },
                new RiskFinding
                {
                    WorkspaceId = beta.WorkspaceId,
                    EventId = betaEventId,
                    EvaluationOutcomeId = betaOutcomeId,
                    ActorUserId = beta.UserId,
                    ToolName = "Claude",
                    Severity = RiskSeverity.High,
                    Reason = "Beta finding"
                });

            return dbContext.SaveChangesAsync();
        });

        await factory.ExecuteServiceAsync<RunUrgentAlertScanService>(service =>
            service.RunAsync(new RunUrgentAlertScanCommand(DateTimeOffset.UtcNow)));
        await factory.ExecuteServiceAsync<DeliverPendingNotificationsService>(service =>
            service.RunAsync(new DeliverPendingNotificationsCommand(DateTimeOffset.UtcNow)));
        await factory.ExecuteServiceAsync<RunUrgentAlertScanService>(service =>
            service.RunAsync(new RunUrgentAlertScanCommand(DateTimeOffset.UtcNow.AddMinutes(1))));
        await factory.ExecuteServiceAsync<DeliverPendingNotificationsService>(service =>
            service.RunAsync(new DeliverPendingNotificationsCommand(DateTimeOffset.UtcNow.AddMinutes(1))));

        var alphaNotifications = await alphaClient.ListNotificationsAsync(alpha.WorkspaceId, type: "urgent_alert");
        var betaNotifications = await betaClient.ListNotificationsAsync(beta.WorkspaceId, type: "urgent_alert");
        var sink = await factory.GetNotificationTestSinkAsync();

        Assert.Single(alphaNotifications.Items);
        Assert.Single(betaNotifications.Items);
        Assert.Equal(2, sink.Messages.Count);
    }
}
