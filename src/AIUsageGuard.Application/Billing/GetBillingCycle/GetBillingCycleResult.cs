using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Billing.GetBillingCycle;

public sealed record GetBillingCycleResult(
    Guid WorkspaceId,
    BillingCycleSummary Cycle,
    IReadOnlyList<LimitEvent> LimitEvents,
    IReadOnlyList<CycleAdjustment> Adjustments);
