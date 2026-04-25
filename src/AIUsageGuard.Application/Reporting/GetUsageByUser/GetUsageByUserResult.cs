using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Reporting.GetUsageByUser;

public sealed record GetUsageByUserResult(
    Guid WorkspaceId,
    ReportingPeriod Period,
    UsageSummaryPage Page);
