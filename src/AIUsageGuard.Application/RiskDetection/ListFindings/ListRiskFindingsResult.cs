using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.ListFindings;

public sealed record ListRiskFindingsResult(
    IReadOnlyList<RiskFinding> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
