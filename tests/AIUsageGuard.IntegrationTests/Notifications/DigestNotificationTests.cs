using AIUsageGuard.Api.Contracts.Notifications;
using AIUsageGuard.Application.BackgroundJobs.RunDigestGeneration;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Notifications.DeliverPendingNotifications;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Notifications;

public sealed class DigestNotificationTests
{
    [Fact]
    public async Task Daily_digest_generation_creates_and_delivers_one_summary_for_completed_period()
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
                IdempotencyKey = "digest-evt-001",
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
                Reason = "High-risk activity",
                DetectedAt = DateTimeOffset.UtcNow.AddDays(-1).AddHours(-2)
            });

            return dbContext.SaveChangesAsync();
        });

        await factory.ExecuteServiceAsync<RunDigestGenerationService>(service =>
            service.RunAsync(new RunDigestGenerationCommand(DateTimeOffset.UtcNow)));
        await factory.ExecuteServiceAsync<DeliverPendingNotificationsService>(service =>
            service.RunAsync(new DeliverPendingNotificationsCommand(DateTimeOffset.UtcNow)));

        var sink = await factory.GetNotificationTestSinkAsync();
        var list = await client.ListNotificationsAsync(session.WorkspaceId, type: "digest");

        Assert.Single(sink.Messages);
        Assert.Single(list.Items);
        Assert.Contains("digest", sink.Messages.Single().Subject, StringComparison.OrdinalIgnoreCase);
    }
}
