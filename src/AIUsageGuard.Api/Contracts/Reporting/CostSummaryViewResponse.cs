namespace AIUsageGuard.Api.Contracts.Reporting;

public sealed record CostSummaryViewResponse(
    decimal EstimatedCostTotal,
    int EventsWithEstimatedCost,
    int EventsMissingEstimatedCost,
    bool IsPartial,
    IReadOnlyList<DailyTrendPointResponse> DailyTrend);
