using AIUsageGuard.Application.Reporting;

namespace AIUsageGuard.Application.Reporting.GetUsageByTool;

public sealed record GetUsageByToolQuery(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    ReportingPeriodQuery Period,
    int PageNumber,
    int PageSize);
