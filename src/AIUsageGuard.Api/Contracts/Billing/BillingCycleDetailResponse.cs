namespace AIUsageGuard.Api.Contracts.Billing;

public sealed record BillingCycleDetailResponse(
    Guid WorkspaceId,
    BillingCycleSummaryResponse Cycle,
    IReadOnlyList<LimitEventResponse> LimitEvents,
    IReadOnlyList<CycleAdjustmentResponse> Adjustments);
