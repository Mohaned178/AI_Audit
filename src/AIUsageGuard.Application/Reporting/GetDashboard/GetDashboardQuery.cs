using AIUsageGuard.Application.Reporting;

namespace AIUsageGuard.Application.Reporting.GetDashboard;

public sealed record GetDashboardQuery(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    ReportingPeriodQuery Period,
    int TopCount = 5);
