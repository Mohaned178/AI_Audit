namespace AIUsageGuard.Application.Billing.ListBillingCycles;

public sealed record ListBillingCyclesQuery(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    int PageNumber,
    int PageSize);
