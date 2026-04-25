namespace AIUsageGuard.Application.Models;

public enum UsageCycleMetricState
{
    WithinLimit = 0,
    Warning = 1,
    Overage = 2,
    Restricted = 3
}
