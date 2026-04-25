using AIUsageGuard.Application.Reporting;

namespace AIUsageGuard.Application.Reporting.GetUsageByUser;

public sealed record GetUsageByUserQuery(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    ReportingPeriodQuery Period,
    int PageNumber,
    int PageSize);
