namespace AIUsageGuard.Application.Models;

public sealed class AlertsSummary
{
    public Guid WorkspaceId { get; set; }

    public ReportingPeriod Period { get; set; } = new();

    public int TotalFindings { get; set; }

    public int HighSeverityCount { get; set; }

    public int MediumSeverityCount { get; set; }

    public int LowSeverityCount { get; set; }

    public int AffectedActorCount { get; set; }

    public int AffectedToolCount { get; set; }

    public IReadOnlyList<DailyTrendPoint> DailyTrend { get; set; } = [];
}
