using AIUsageGuard.Application.Reporting;

namespace AIUsageGuard.Application.Reporting.GetAlertsSummary;

public sealed record GetAlertsSummaryQuery(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    ReportingPeriodQuery Period);
