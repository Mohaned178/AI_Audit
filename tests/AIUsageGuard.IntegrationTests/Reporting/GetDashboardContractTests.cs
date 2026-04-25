using System.Net;
using AIUsageGuard.Api.Contracts.Reporting;
using AIUsageGuard.IntegrationTests.Infrastructure;

namespace AIUsageGuard.IntegrationTests.Reporting;

public sealed class GetDashboardContractTests
{
    [Fact]
    public async Task Get_rejects_reversed_period_with_problem_details()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var response = await client.GetDashboardResponseAsync(
            session.WorkspaceId,
            new ReportingPeriodRequest(new DateOnly(2026, 4, 21), new DateOnly(2026, 4, 1)));

        var problem = await response.ReadProblemAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(400, problem.Status);
        Assert.Contains("fromDate", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_returns_period_totals_and_nested_summary_shapes()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = await factory.CreateInitializedApiClientAsync();

        var session = await client.RegisterWorkspaceAsync(
            "owner@example.com",
            "Password123!",
            "Owner",
            "Alpha Workspace");

        var occurredAt = new DateTimeOffset(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.AIUsageEvents.Add(new AIUsageGuard.Application.Models.AIUsageEvent
            {
                WorkspaceId = session.WorkspaceId,
                ActorUserId = session.UserId,
                EventType = AIUsageGuard.Application.Models.AIUsageEventType.PromptSubmitted,
                IdempotencyKey = "evt-contract-dashboard-001",
                ToolName = "ChatGPT",
                OccurredAt = occurredAt,
                ReceivedAt = occurredAt,
                EstimatedCost = 1.25m
            });
            await dbContext.SaveChangesAsync();
        });

        var response = await client.GetDashboardAsync(
            session.WorkspaceId,
            new ReportingPeriodRequest(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 21)));

        Assert.Equal(session.WorkspaceId, response.WorkspaceId);
        Assert.Equal(new DateOnly(2026, 4, 1), response.Period.FromDate);
        Assert.Equal(21, response.Period.DayCount);
        Assert.True(response.Totals.TotalEvents >= 1);
        Assert.NotNull(response.TopUsers);
        Assert.NotNull(response.TopTools);
        Assert.NotNull(response.Alerts.DailyTrend);
        Assert.NotNull(response.Costs.DailyTrend);
    }
}
