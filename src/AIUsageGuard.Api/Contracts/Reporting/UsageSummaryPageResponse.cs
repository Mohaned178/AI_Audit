namespace AIUsageGuard.Api.Contracts.Reporting;

public sealed record UsageSummaryPageResponse(
    IReadOnlyList<UsageSummaryRowResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
