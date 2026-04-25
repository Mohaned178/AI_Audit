using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Reporting.GetCostSummary;

public sealed record GetCostSummaryResult(
    Guid WorkspaceId,
    ReportingPeriod Period,
    EstimatedCostSummary Summary);
