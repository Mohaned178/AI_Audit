using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Reporting.GetUsageByTool;

public sealed record GetUsageByToolResult(
    Guid WorkspaceId,
    ReportingPeriod Period,
    UsageSummaryPage Page);
