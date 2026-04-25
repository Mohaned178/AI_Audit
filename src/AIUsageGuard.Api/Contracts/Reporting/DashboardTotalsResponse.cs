namespace AIUsageGuard.Api.Contracts.Reporting;

public sealed record DashboardTotalsResponse(
    int TotalEvents,
    int UniqueActorCount,
    int UniqueToolCount,
    int FlaggedFindingCount);
