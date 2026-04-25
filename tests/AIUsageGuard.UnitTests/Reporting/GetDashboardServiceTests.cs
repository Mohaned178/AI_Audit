using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Reporting;
using AIUsageGuard.Application.Reporting.GetDashboard;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageGuard.UnitTests.Reporting;

public sealed class GetDashboardServiceTests
{
    [Fact]
    public async Task Get_returns_workspace_scoped_totals_rankings_and_partial_costs()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var outsideWorkspaceId = Guid.NewGuid();
        await SeedWorkspaceAsync(dbContext, workspaceId, ownerId, memberId);
        await SeedWorkspaceAsync(dbContext, outsideWorkspaceId, Guid.NewGuid(), null);

        var inRangeDay1 = new DateTimeOffset(2026, 4, 5, 10, 0, 0, TimeSpan.Zero);
        var inRangeDay2 = new DateTimeOffset(2026, 4, 6, 9, 0, 0, TimeSpan.Zero);
        var inRangeDay3 = new DateTimeOffset(2026, 4, 7, 15, 30, 0, TimeSpan.Zero);

        var ownerEventOne = CreateEvent(workspaceId, ownerId, "evt-001", "ChatGPT", inRangeDay1, 10m);
        var memberEvent = CreateEvent(workspaceId, memberId, "evt-002", "Claude", inRangeDay2, null);
        var ownerEventTwo = CreateEvent(workspaceId, ownerId, "evt-003", "ChatGPT", inRangeDay3, 5m);
        var outsideRangeEvent = CreateEvent(workspaceId, ownerId, "evt-004", "ChatGPT", new DateTimeOffset(2026, 3, 31, 23, 59, 0, TimeSpan.Zero), 99m);
        var otherWorkspaceEvent = CreateEvent(outsideWorkspaceId, ownerId, "evt-005", "Gemini", inRangeDay2, 88m);

        dbContext.AIUsageEvents.AddRange(ownerEventOne, memberEvent, ownerEventTwo, outsideRangeEvent, otherWorkspaceEvent);

        var outcomeOne = CreateOutcome(workspaceId, memberEvent.Id, inRangeDay2);
        var outcomeTwo = CreateOutcome(workspaceId, ownerEventTwo.Id, inRangeDay3);
        dbContext.RiskEvaluationOutcomes.AddRange(outcomeOne, outcomeTwo);
        dbContext.RiskFindings.AddRange(
            CreateFinding(workspaceId, memberEvent, outcomeOne.Id, RiskSeverity.High, RiskRuleType.SensitiveDataPattern, inRangeDay2),
            CreateFinding(workspaceId, ownerEventTwo, outcomeTwo.Id, RiskSeverity.Medium, RiskRuleType.CostThresholdExceeded, inRangeDay3));

        await dbContext.SaveChangesAsync();

        var service = new GetDashboardService(dbContext, new AuditService(dbContext), NullLogger<GetDashboardService>.Instance);

        var result = await service.GetAsync(new GetDashboardQuery(
            workspaceId,
            ownerId,
            new ReportingPeriodQuery(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7))));

        Assert.Equal(3, result.Summary.Totals.TotalEvents);
        Assert.Equal(2, result.Summary.Totals.UniqueActorCount);
        Assert.Equal(2, result.Summary.Totals.UniqueToolCount);
        Assert.Equal(2, result.Summary.Totals.FlaggedFindingCount);
        Assert.Equal(2, result.Summary.TopUsers.TotalCount);
        Assert.Equal("Owner User", result.Summary.TopUsers.Items[0].DisplayLabel);
        Assert.Equal(2, result.Summary.TopUsers.Items[0].TotalEvents);
        Assert.Equal("ChatGPT", result.Summary.TopTools.Items[0].DisplayLabel);
        Assert.Equal(15m, result.Summary.Costs.EstimatedCostTotal);
        Assert.True(result.Summary.Costs.IsPartial);
        Assert.Equal(1, result.Summary.Alerts.HighSeverityCount);
        Assert.Equal(1, result.Summary.Alerts.MediumSeverityCount);
        Assert.Equal(7, result.Summary.Alerts.DailyTrend.Count);
        Assert.Equal(1, await dbContext.AuditRecords.CountAsync(item => item.ActionType == "dashboard.read" && item.Result == "success"));
    }

    [Fact]
    public async Task Get_rejects_invalid_period_and_records_failed_audit()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        await SeedWorkspaceAsync(dbContext, workspaceId, ownerId, null);

        var service = new GetDashboardService(dbContext, new AuditService(dbContext), NullLogger<GetDashboardService>.Instance);

        var exception = await Assert.ThrowsAsync<AIUsageGuard.Application.Errors.RequestFailureException>(() =>
            service.GetAsync(new GetDashboardQuery(
                workspaceId,
                ownerId,
                new ReportingPeriodQuery(new DateOnly(2026, 4, 7), new DateOnly(2026, 4, 1)))));

        Assert.Equal(400, exception.StatusCode);
        Assert.Equal(1, await dbContext.AuditRecords.CountAsync(item => item.ActionType == "dashboard.read" && item.Result == "failed"));
    }

    private static async Task SeedWorkspaceAsync(
        AIUsageGuard.Infrastructure.Persistence.ApplicationDbContext dbContext,
        Guid workspaceId,
        Guid ownerId,
        Guid? memberId)
    {
        dbContext.Workspaces.Add(new Workspace
        {
            Id = workspaceId,
            Name = $"Workspace-{workspaceId:N}",
            Slug = $"workspace-{workspaceId:N}",
            CreatedByUserId = ownerId
        });
        await dbContext.AddUserAsync(new UserAccount
        {
            Id = ownerId,
            Email = $"owner-{ownerId:N}@example.com",
            DisplayName = "Owner User",
            PasswordHash = "hash"
        });
        await dbContext.AddMembershipAsync(new WorkspaceMembership
        {
            WorkspaceId = workspaceId,
            UserId = ownerId,
            Role = WorkspaceRole.Owner,
            Status = MembershipStatus.Active
        });

        if (!memberId.HasValue)
        {
            return;
        }

        await dbContext.AddUserAsync(new UserAccount
        {
            Id = memberId.Value,
            Email = $"member-{memberId.Value:N}@example.com",
            DisplayName = "Member User",
            PasswordHash = "hash"
        });
        await dbContext.AddMembershipAsync(new WorkspaceMembership
        {
            WorkspaceId = workspaceId,
            UserId = memberId.Value,
            Role = WorkspaceRole.Member,
            Status = MembershipStatus.Active
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
            ReceivedAt = occurredAt.AddMinutes(1),
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
