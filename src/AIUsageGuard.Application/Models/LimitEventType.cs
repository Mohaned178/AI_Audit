namespace AIUsageGuard.Application.Models;

public enum LimitEventType
{
    WarningRaised = 0,
    OverageStarted = 1,
    RestrictionApplied = 2,
    StateReturnedToWithinLimit = 3
}
