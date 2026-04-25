namespace AIUsageGuard.Application.Billing.ReconcileUsageCycles;

public sealed record ReconcileUsageCyclesResult(
    int WorkspacesProcessed,
    int CyclesAdjusted,
    int AdjustmentsRecorded);
