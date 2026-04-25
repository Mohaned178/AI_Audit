using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.RiskDetection.EvaluateEvent;
using AIUsageGuard.Infrastructure.Persistence;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.UnitTests.RiskDetection;

public sealed class EvaluateAIUsageEventRiskServiceTests
{
    [Fact]
    public async Task Evaluate_persists_outcome_and_findings_for_matching_event()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        SeedWorkspace(dbContext, workspaceId, actorId);
        var eventRecord = new AIUsageEvent
        {
            WorkspaceId = workspaceId,
            ActorUserId = actorId,
            EventType = AIUsageEventType.PromptSubmitted,
            IdempotencyKey = "evt-risk-001",
            ToolName = "Claude",
            PromptPreview = "Contact owner@example.com",
            EstimatedCost = 20m,
            OccurredAt = DateTimeOffset.UtcNow
        };
        dbContext.AIUsageEvents.Add(eventRecord);
        dbContext.WorkspaceRiskPolicies.Add(new WorkspaceRiskPolicy
        {
            WorkspaceId = workspaceId,
            ApprovedTools = ["ChatGPT"],
            PerEventEstimatedCostThreshold = 5m,
            LastUpdatedByUserId = actorId
        });
        await dbContext.SaveChangesAsync();

        var service = RiskDetectionTestFactory.CreateEvaluationService(dbContext);

        var result = await service.EvaluateAsync(new EvaluateAIUsageEventRiskCommand(workspaceId, eventRecord.Id, actorId));

        Assert.True(result.CreatedNewOutcome);
        Assert.Equal(RiskEvaluationResult.Matched, result.Outcome.EvaluationResult);
        Assert.Equal(3, result.Outcome.MatchedRuleCount);
        Assert.Equal(3, await dbContext.RiskFindings.CountAsync());
        Assert.Equal(1, await dbContext.RiskEvaluationOutcomes.CountAsync());
        Assert.Equal(4, await dbContext.AuditRecords.CountAsync());
    }

    [Fact]
    public async Task Evaluate_persists_no_match_outcome_without_findings_for_clean_event()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        SeedWorkspace(dbContext, workspaceId, actorId);
        var eventRecord = new AIUsageEvent
        {
            WorkspaceId = workspaceId,
            ActorUserId = actorId,
            EventType = AIUsageEventType.PromptSubmitted,
            IdempotencyKey = "evt-clean-001",
            ToolName = "ChatGPT",
            PromptPreview = "Summarize the sprint notes.",
            EstimatedCost = 0.2m,
            OccurredAt = DateTimeOffset.UtcNow
        };
        dbContext.AIUsageEvents.Add(eventRecord);
        dbContext.WorkspaceRiskPolicies.Add(new WorkspaceRiskPolicy
        {
            WorkspaceId = workspaceId,
            ApprovedTools = ["ChatGPT"],
            PerEventEstimatedCostThreshold = 5m,
            LastUpdatedByUserId = actorId
        });
        await dbContext.SaveChangesAsync();

        var service = RiskDetectionTestFactory.CreateEvaluationService(dbContext);

        var result = await service.EvaluateAsync(new EvaluateAIUsageEventRiskCommand(workspaceId, eventRecord.Id, actorId));

        Assert.True(result.CreatedNewOutcome);
        Assert.Equal(RiskEvaluationResult.NoMatch, result.Outcome.EvaluationResult);
        Assert.Empty(result.Findings);
        Assert.Equal(0, await dbContext.RiskFindings.CountAsync());
        Assert.Equal(1, await dbContext.RiskEvaluationOutcomes.CountAsync());
    }

    [Fact]
    public async Task Evaluate_returns_existing_outcome_without_creating_duplicates_on_replay()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        SeedWorkspace(dbContext, workspaceId, actorId);
        var eventRecord = new AIUsageEvent
        {
            WorkspaceId = workspaceId,
            ActorUserId = actorId,
            EventType = AIUsageEventType.FileUploaded,
            IdempotencyKey = "evt-replay-001",
            ToolName = "ChatGPT",
            FileName = "source.docx",
            OccurredAt = DateTimeOffset.UtcNow
        };
        dbContext.AIUsageEvents.Add(eventRecord);
        await dbContext.SaveChangesAsync();

        var service = RiskDetectionTestFactory.CreateEvaluationService(dbContext);

        var first = await service.EvaluateAsync(new EvaluateAIUsageEventRiskCommand(workspaceId, eventRecord.Id, actorId));
        var second = await service.EvaluateAsync(new EvaluateAIUsageEventRiskCommand(workspaceId, eventRecord.Id, actorId));

        Assert.True(first.CreatedNewOutcome);
        Assert.False(second.CreatedNewOutcome);
        Assert.Equal(first.Outcome.Id, second.Outcome.Id);
        Assert.Equal(1, await dbContext.RiskEvaluationOutcomes.CountAsync());
        Assert.Equal(1, await dbContext.RiskFindings.CountAsync());
    }

    private static void SeedWorkspace(ApplicationDbContext dbContext, Guid workspaceId, Guid actorId)
    {
        dbContext.Workspaces.Add(new Workspace
        {
            Id = workspaceId,
            Name = "Alpha Workspace",
            Slug = $"alpha-{workspaceId:N}",
            CreatedByUserId = actorId
        });
        dbContext.SaveChanges();
    }
}
