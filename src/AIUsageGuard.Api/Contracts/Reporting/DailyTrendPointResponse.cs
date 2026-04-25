namespace AIUsageGuard.Api.Contracts.Reporting;

public sealed record DailyTrendPointResponse(
    DateOnly Date,
    int EventCount,
    int FindingCount,
    decimal EstimatedCost);
