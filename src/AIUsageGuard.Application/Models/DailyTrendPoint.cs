namespace AIUsageGuard.Application.Models;

public sealed class DailyTrendPoint
{
    public DateOnly Date { get; set; }

    public int EventCount { get; set; }

    public int FindingCount { get; set; }

    public decimal EstimatedCost { get; set; }
}
