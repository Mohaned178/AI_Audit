using AIUsageGuard.Application.Reporting;

namespace AIUsageGuard.Application.Reporting.GetCostSummary;

public sealed record GetCostSummaryQuery(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    ReportingPeriodQuery Period);
