using AIUsageGuard.Application.Reporting;
using AIUsageGuard.Application.Reporting.GetCostSummary;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageGuard.UnitTests.Reporting;

public sealed class GetCostSummaryServiceTests
{
    [Fact]
    public async Task Get_returns_cost_totals_partial_state_and_daily_trend()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        await ReportingTestData.SeedWorkspaceAsync(dbContext, workspaceId, ownerId, null);

        var day1 = new DateTimeOffset(2026, 4, 5, 10, 0, 0, TimeSpan.Zero);
        var day2 = new DateTimeOffset(2026, 4, 6, 11, 0, 0, TimeSpan.Zero);
        dbContext.AIUsageEvents.AddRange(
            ReportingTestData.CreateEvent(workspaceId, ownerId, "evt-c-001", "ChatGPT", day1, 10m),
            ReportingTestData.CreateEvent(workspaceId, ownerId, "evt-c-002", "Claude", day2, null));
        await dbContext.SaveChangesAsync();

        var service = new GetCostSummaryService(dbContext, new AuditService(dbContext), NullLogger<GetCostSummaryService>.Instance);

        var result = await service.GetAsync(new GetCostSummaryQuery(
            workspaceId,
            ownerId,
            new ReportingPeriodQuery(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 7))));

        Assert.Equal(10m, result.Summary.EstimatedCostTotal);
        Assert.Equal(1, result.Summary.EventsWithEstimatedCost);
        Assert.Equal(1, result.Summary.EventsMissingEstimatedCost);
        Assert.True(result.Summary.IsPartial);
        Assert.Equal(7, result.Summary.DailyTrend.Count);
        Assert.Equal(1, await dbContext.AuditRecords.CountAsync(item => item.ActionType == "report.cost_summary.read" && item.Result == "success"));
    }
}
