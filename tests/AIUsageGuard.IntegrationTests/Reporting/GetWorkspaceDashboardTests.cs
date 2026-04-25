using AIUsageGuard.Api.Contracts.Reporting;
using AIUsageGuard.Application.Models;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Reporting;

public sealed class GetWorkspaceDashboardTests
{
    [Fact]
    public async Task Owner_can_view_workspace_dashboard_summary_for_selected_period()
    {
        await using var factory = new TestWebApplicationFactory();
        using var ownerClient = await factory.CreateInitializedApiClientAsync();
        using var memberClient = await factory.CreateInitializedApiClientAsync();
        using var outsiderClient = await factory.CreateInitializedApiClientAsync();

        var ownerSession = await ownerClient.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var memberSession = await memberClient.RegisterWorkspaceAsync(
            "member@example.com",
            "Password123!",
            "Member",
            "Beta Workspace");

        var outsiderSession = await outsiderClient.RegisterWorkspaceAsync(
            "outsider@example.com",
            "Password123!",
            "Outsider",
            "Gamma Workspace");

        await ownerClient.CreateMembershipAsync(ownerSession.WorkspaceId, "member@example.com", "Member");

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            var inRangeDay1 = new DateTimeOffset(2026, 4, 5, 8, 0, 0, TimeSpan.Zero);
            var inRangeDay2 = new DateTimeOffset(2026, 4, 6, 13, 0, 0, TimeSpan.Zero);
            var inRangeDay3 = new DateTimeOffset(2026, 4, 7, 17, 15, 0, TimeSpan.Zero);

            var ownerEventOne = CreateEvent(ownerSession.WorkspaceId, ownerSession.UserId, "evt-int-001", "ChatGPT", inRangeDay1, 10m);
            var memberEvent = CreateEvent(ownerSession.WorkspaceId, memberSession.UserId, "evt-int-002", "Claude", inRangeDay2, null);
            var ownerEventTwo = CreateEvent(ownerSession.WorkspaceId, ownerSession.UserId, "evt-int-003", "ChatGPT", inRangeDay3, 5m);
            var outsideRangeEvent = CreateEvent(ownerSession.WorkspaceId, ownerSession.UserId, "evt-int-004", "ChatGPT", new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero), 99m);
            var outsiderEvent = CreateEvent(outsiderSession.WorkspaceId, outsiderSession.UserId, "evt-int-005", "Gemini", inRangeDay2, 88m);

            dbContext.AIUsageEvents.AddRange(ownerEventOne, memberEvent, ownerEventTwo, outsideRangeEvent, outsiderEvent);

            var outcomeOne = CreateOutcome(ownerSession.WorkspaceId, memberEvent.Id, inRangeDay2);
            var outcomeTwo = CreateOutcome(ownerSession.WorkspaceId, ownerEventTwo.Id, inRangeDay3);
            dbContext.RiskEvaluationOutcomes.AddRange(outcomeOne, outcomeTwo);
            dbContext.RiskFindings.AddRange(
                CreateFinding(ownerSession.WorkspaceId, memberEvent, outcomeOne.Id, RiskSeverity.High, RiskRuleType.SensitiveDataPattern, inRangeDay2),
                CreateFinding(ownerSession.WorkspaceId, ownerEventTwo, outcomeTwo.Id, RiskSeverity.Medium, RiskRuleType.CostThresholdExceeded, inRangeDay3));

            await dbContext.SaveChangesAsync();
        });

        var response = await ownerClient.GetDashboardAsync(
            ownerSession.WorkspaceId,
            new ReportingPeriodRequest(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7)));

        Assert.Equal(3, response.Totals.TotalEvents);
        Assert.Equal(2, response.Totals.UniqueActorCount);
        Assert.Equal(2, response.Totals.UniqueToolCount);
        Assert.Equal(2, response.Totals.FlaggedFindingCount);
        Assert.Equal(2, response.TopUsers.TotalCount);
        Assert.Equal("Owner", response.TopUsers.Items[0].DisplayLabel);
        Assert.Equal("ChatGPT", response.TopTools.Items[0].DisplayLabel);
        Assert.Equal(15m, response.Costs.EstimatedCostTotal);
        Assert.True(response.Costs.IsPartial);
        Assert.Equal(1, response.Alerts.HighSeverityCount);
        Assert.Equal(1, response.Alerts.MediumSeverityCount);
        Assert.Equal(7, response.Alerts.DailyTrend.Count);
    }

    private static AIUsageEvent CreateEvent(
        Guid workspaceId,
        Guid actorUserId,
        string idempotencyKey,
        string toolName,
        DateTimeOffset occurredAt,
        decimal? estimatedCost)
    {
        return new AIUsageEvent
        {
            WorkspaceId = workspaceId,
            ActorUserId = actorUserId,
            EventType = AIUsageEventType.PromptSubmitted,
            IdempotencyKey = idempotencyKey,
            ToolName = toolName,
            OccurredAt = occurredAt,
            ReceivedAt = occurredAt,
            EstimatedCost = estimatedCost
        };
    }

    private static RiskEvaluationOutcome CreateOutcome(Guid workspaceId, Guid eventId, DateTimeOffset evaluatedAt)
    {
        return new RiskEvaluationOutcome
        {
            WorkspaceId = workspaceId,
            EventId = eventId,
            EvaluatedAt = evaluatedAt,
            AppliedRuleVersion = "test",
            MatchedRuleCount = 1,
            EvaluationResult = RiskEvaluationResult.Matched,
            EvidenceSummary = "Matched"
        };
    }

    private static RiskFinding CreateFinding(
        Guid workspaceId,
        AIUsageEvent eventRecord,
        Guid outcomeId,
        RiskSeverity severity,
        RiskRuleType ruleType,
        DateTimeOffset detectedAt)
    {
        return new RiskFinding
        {
            WorkspaceId = workspaceId,
            EventId = eventRecord.Id,
            EvaluationOutcomeId = outcomeId,
            RuleType = ruleType,
            Severity = severity,
            Status = RiskFindingStatus.Open,
            Reason = "Matched rule.",
            ActorUserId = eventRecord.ActorUserId,
            ToolName = eventRecord.ToolName,
            DetectedAt = detectedAt
        };
    }
}
