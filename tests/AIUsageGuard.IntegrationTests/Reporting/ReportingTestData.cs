using AIUsageGuard.Api.Contracts.Auth;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Reporting;

internal static class ReportingTestData
{
    public static async Task SeedScenarioAsync(
        TestWebApplicationFactory factory,
        WorkspaceSessionResponse ownerSession,
        WorkspaceSessionResponse memberSession,
        WorkspaceSessionResponse outsiderSession)
    {
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
    }

    public static async Task SeedEmptyScenarioAsync(
        TestWebApplicationFactory factory,
        Guid workspaceId,
        Guid userId)
    {
        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            var workspace = await dbContext.Workspaces.FindAsync(workspaceId);
            if (workspace is null)
            {
                dbContext.Workspaces.Add(new Workspace
                {
                    Id = workspaceId,
                    Name = "Workspace",
                    Slug = $"workspace-{workspaceId:N}",
                    CreatedByUserId = userId
                });
                await dbContext.SaveChangesAsync();
            }
        });
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
