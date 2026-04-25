using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Billing.GetPlanStatus;

public sealed record GetPlanStatusResult(
    PlanStatusSnapshot Snapshot,
    UsageCycle CurrentCycle,
    DateTimeOffset AssignedFromCycleStartUtc);
