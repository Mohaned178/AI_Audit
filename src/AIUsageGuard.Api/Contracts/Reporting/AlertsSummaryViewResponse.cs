namespace AIUsageGuard.Api.Contracts.Reporting;

public sealed record AlertsSummaryViewResponse(
    int TotalFindings,
    int HighSeverityCount,
    int MediumSeverityCount,
    int LowSeverityCount,
    int AffectedActorCount,
    int AffectedToolCount,
    IReadOnlyList<DailyTrendPointResponse> DailyTrend);
