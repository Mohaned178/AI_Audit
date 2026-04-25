using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Persistence;

namespace AIUsageGuard.UnitTests.Reporting;

internal static class ReportingTestData
{
    public static async Task SeedWorkspaceAsync(
        ApplicationDbContext dbContext,
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

    public static AIUsageEvent CreateEvent(
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

    public static RiskEvaluationOutcome CreateOutcome(Guid workspaceId, Guid eventId, DateTimeOffset evaluatedAt)
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

    public static RiskFinding CreateFinding(
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
