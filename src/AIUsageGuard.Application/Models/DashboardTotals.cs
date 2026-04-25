namespace AIUsageGuard.Application.Models;

public sealed class DashboardTotals
{
    public int TotalEvents { get; set; }

    public int UniqueActorCount { get; set; }

    public int UniqueToolCount { get; set; }

    public int FlaggedFindingCount { get; set; }
}
