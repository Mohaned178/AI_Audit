using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Reporting;
using AIUsageGuard.Application.Reporting.GetUsageByTool;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageGuard.UnitTests.Reporting;

public sealed class GetUsageByToolServiceTests
{
    [Fact]
    public async Task Get_groups_usage_by_tool_and_keeps_total_count()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        await ReportingTestData.SeedWorkspaceAsync(dbContext, workspaceId, ownerId, null);

        var day1 = new DateTimeOffset(2026, 4, 5, 10, 0, 0, TimeSpan.Zero);
        var day2 = new DateTimeOffset(2026, 4, 6, 11, 0, 0, TimeSpan.Zero);
        var day3 = new DateTimeOffset(2026, 4, 7, 12, 0, 0, TimeSpan.Zero);
        var first = ReportingTestData.CreateEvent(workspaceId, ownerId, "evt-t-001", "ChatGPT", day1, 10m);
        var second = ReportingTestData.CreateEvent(workspaceId, ownerId, "evt-t-002", "Claude", day2, null);
        var third = ReportingTestData.CreateEvent(workspaceId, ownerId, "evt-t-003", "ChatGPT", day3, 5m);
        dbContext.AIUsageEvents.AddRange(first, second, third);

        var outcome = ReportingTestData.CreateOutcome(workspaceId, second.Id, day2);
        dbContext.RiskEvaluationOutcomes.Add(outcome);
        dbContext.RiskFindings.Add(ReportingTestData.CreateFinding(workspaceId, second, outcome.Id, RiskSeverity.High, RiskRuleType.SensitiveDataPattern, day2));
        await dbContext.SaveChangesAsync();

        var service = new GetUsageByToolService(dbContext, new AuditService(dbContext), NullLogger<GetUsageByToolService>.Instance);

        var result = await service.GetAsync(new GetUsageByToolQuery(
            workspaceId,
            ownerId,
            new ReportingPeriodQuery(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7)),
            1,
            10));

        Assert.Equal(2, result.Page.TotalCount);
        Assert.Equal("ChatGPT", result.Page.Items[0].DisplayLabel);
        Assert.Equal(2, result.Page.Items[0].TotalEvents);
        Assert.Equal("Claude", result.Page.Items[1].DisplayLabel);
        Assert.Equal(1, result.Page.Items[1].FlaggedFindingCount);
        Assert.Equal(1, await dbContext.AuditRecords.CountAsync(item => item.ActionType == "report.usage_by_tool.read" && item.Result == "success"));
    }
}
