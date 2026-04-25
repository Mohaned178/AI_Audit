namespace AIUsageGuard.Application.Models;

public sealed class ReportingPeriod
{
    public DateOnly FromDate { get; set; }

    public DateOnly ToDate { get; set; }

    public DateTimeOffset NormalizedFromUtc { get; set; }

    public DateTimeOffset NormalizedToUtc { get; set; }

    public int DayCount { get; set; }
}
