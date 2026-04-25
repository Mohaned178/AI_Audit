namespace AIUsageGuard.Application.Billing.ReconcileUsageCycles;

public sealed record ReconcileUsageCyclesCommand(DateTimeOffset ScheduledForUtc);
