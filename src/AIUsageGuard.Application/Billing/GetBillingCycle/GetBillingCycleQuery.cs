namespace AIUsageGuard.Application.Billing.GetBillingCycle;

public sealed record GetBillingCycleQuery(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    Guid UsageCycleId);
