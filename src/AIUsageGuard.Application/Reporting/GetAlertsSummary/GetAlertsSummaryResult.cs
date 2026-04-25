using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Reporting.GetAlertsSummary;

public sealed record GetAlertsSummaryResult(
    Guid WorkspaceId,
    ReportingPeriod Period,
    AlertsSummary Summary);
