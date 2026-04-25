using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Reporting;
using AIUsageGuard.Application.Reporting.GetUsageByUser;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageGuard.UnitTests.Reporting;

public sealed class GetUsageByUserServiceTests
{
    [Fact]
    public async Task Get_groups_usage_by_user_and_respects_paging()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        await ReportingTestData.SeedWorkspaceAsync(dbContext, workspaceId, ownerId, memberId);

        var day1 = new DateTimeOffset(2026, 4, 5, 10, 0, 0, TimeSpan.Zero);
        var day2 = new DateTimeOffset(2026, 4, 6, 11, 0, 0, TimeSpan.Zero);
        var day3 = new DateTimeOffset(2026, 4, 7, 12, 0, 0, TimeSpan.Zero);
        var ownerEventOne = ReportingTestData.CreateEvent(workspaceId, ownerId, "evt-u-001", "ChatGPT", day1, 10m);
        var ownerEventTwo = ReportingTestData.CreateEvent(workspaceId, ownerId, "evt-u-002", "ChatGPT", day3, 5m);
        var memberEvent = ReportingTestData.CreateEvent(workspaceId, memberId, "evt-u-003", "Claude", day2, null);
        dbContext.AIUsageEvents.AddRange(ownerEventOne, ownerEventTwo, memberEvent);

        var ownerOutcome = ReportingTestData.CreateOutcome(workspaceId, ownerEventTwo.Id, day3);
        var memberOutcome = ReportingTestData.CreateOutcome(workspaceId, memberEvent.Id, day2);
        dbContext.RiskEvaluationOutcomes.AddRange(ownerOutcome, memberOutcome);
        dbContext.RiskFindings.AddRange(
            ReportingTestData.CreateFinding(workspaceId, ownerEventTwo, ownerOutcome.Id, RiskSeverity.Medium, RiskRuleType.CostThresholdExceeded, day3),
            ReportingTestData.CreateFinding(workspaceId, memberEvent, memberOutcome.Id, RiskSeverity.High, RiskRuleType.SensitiveDataPattern, day2));
        await dbContext.SaveChangesAsync();

        var service = new GetUsageByUserService(dbContext, new AuditService(dbContext), NullLogger<GetUsageByUserService>.Instance);

        var result = await service.GetAsync(new GetUsageByUserQuery(
            workspaceId,
            ownerId,
            new ReportingPeriodQuery(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7)),
            1,
            1));

        Assert.Equal(2, result.Page.TotalCount);
        Assert.Single(result.Page.Items);
        Assert.Equal("Owner User", result.Page.Items[0].DisplayLabel);
        Assert.Equal(2, result.Page.Items[0].TotalEvents);
        Assert.Equal(1, result.Page.Items[0].FlaggedFindingCount);
        Assert.Equal(1, await dbContext.AuditRecords.CountAsync(item => item.ActionType == "report.usage_by_user.read" && item.Result == "success"));
    }
}
