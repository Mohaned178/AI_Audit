namespace AIUsageGuard.Api.Contracts.Reporting;

public sealed record DashboardResponse(
    Guid WorkspaceId,
    ReportingPeriodResponse Period,
    DashboardTotalsResponse Totals,
    UsageSummaryPageResponse TopUsers,
    UsageSummaryPageResponse TopTools,
    AlertsSummaryViewResponse Alerts,
    CostSummaryViewResponse Costs);
