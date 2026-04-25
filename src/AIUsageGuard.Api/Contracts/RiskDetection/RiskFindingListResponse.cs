namespace AIUsageGuard.Api.Contracts.RiskDetection;

public sealed record RiskFindingListResponse(
    IReadOnlyList<RiskFindingListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
