using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Billing.ListBillingCycles;

public sealed record ListBillingCyclesResult(
    Guid WorkspaceId,
    int PageNumber,
    int PageSize,
    int TotalCount,
    IReadOnlyList<BillingCycleSummary> Items);
