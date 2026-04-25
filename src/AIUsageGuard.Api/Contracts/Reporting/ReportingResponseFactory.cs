using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Api.Contracts.Reporting;

internal static class ReportingResponseFactory
{
    public static DashboardResponse ToDashboardResponse(DashboardSummary summary)
    {
        return new DashboardResponse(
            summary.WorkspaceId,
            ToPeriodResponse(summary.Period),
            new DashboardTotalsResponse(
                summary.Totals.TotalEvents,
                summary.Totals.UniqueActorCount,
                summary.Totals.UniqueToolCount,
                summary.Totals.FlaggedFindingCount),
            ToUsageSummaryPage(summary.TopUsers),
            ToUsageSummaryPage(summary.TopTools),
            ToAlertsSummaryView(summary.Alerts),
            ToCostSummaryView(summary.Costs));
    }

    public static UsageSummaryResponse ToUsageSummaryResponse(
        Guid workspaceId,
        ReportingPeriod period,
        string groupedBy,
        UsageSummaryPage page)
        => new(
            workspaceId,
            ToPeriodResponse(period),
            groupedBy,
            ToUsageSummaryPage(page));

    public static AlertsSummaryResponse ToAlertsSummaryResponse(
        Guid workspaceId,
        ReportingPeriod period,
        AlertsSummary summary)
        => new(
            workspaceId,
            ToPeriodResponse(period),
            ToAlertsSummaryView(summary));

    public static CostSummaryResponse ToCostSummaryResponse(
        Guid workspaceId,
        ReportingPeriod period,
        EstimatedCostSummary summary)
        => new(
            workspaceId,
            ToPeriodResponse(period),
            ToCostSummaryView(summary));

    public static ReportingPeriodResponse ToPeriodResponse(ReportingPeriod period)
        => new(period.FromDate, period.ToDate, period.DayCount);

    public static UsageSummaryPageResponse ToUsageSummaryPage(UsageSummaryPage page)
        => new(
            page.Items.Select(item => new UsageSummaryRowResponse(
                item.ActorUserId,
                item.DisplayLabel,
                item.TotalEvents,
                item.FlaggedFindingCount,
                item.EstimatedCost,
                item.EventsWithEstimatedCost,
                item.EventsMissingEstimatedCost,
                item.LastActivityAt)).ToList(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount);

    public static AlertsSummaryViewResponse ToAlertsSummaryView(AlertsSummary summary)
        => new(
            summary.TotalFindings,
            summary.HighSeverityCount,
            summary.MediumSeverityCount,
            summary.LowSeverityCount,
            summary.AffectedActorCount,
            summary.AffectedToolCount,
            ToDailyTrend(summary.DailyTrend));

    public static CostSummaryViewResponse ToCostSummaryView(EstimatedCostSummary summary)
        => new(
            summary.EstimatedCostTotal,
            summary.EventsWithEstimatedCost,
            summary.EventsMissingEstimatedCost,
            summary.IsPartial,
            ToDailyTrend(summary.DailyTrend));

    private static IReadOnlyList<DailyTrendPointResponse> ToDailyTrend(IReadOnlyList<DailyTrendPoint> items)
        => items
            .Select(item => new DailyTrendPointResponse(
                item.Date,
                item.EventCount,
                item.FindingCount,
                item.EstimatedCost))
            .ToList();
}
