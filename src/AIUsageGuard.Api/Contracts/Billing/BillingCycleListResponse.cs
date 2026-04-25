namespace AIUsageGuard.Api.Contracts.Billing;

public sealed record BillingCyclePageResponse(
    IReadOnlyList<BillingCycleSummaryResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record BillingCycleListResponse(
    Guid WorkspaceId,
    BillingCyclePageResponse Page);
