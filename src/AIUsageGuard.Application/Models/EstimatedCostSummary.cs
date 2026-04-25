namespace AIUsageGuard.Application.Models;

public sealed class EstimatedCostSummary
{
    public Guid WorkspaceId { get; set; }

    public ReportingPeriod Period { get; set; } = new();

    public decimal EstimatedCostTotal { get; set; }

    public int EventsWithEstimatedCost { get; set; }

    public int EventsMissingEstimatedCost { get; set; }

    public bool IsPartial { get; set; }

    public IReadOnlyList<DailyTrendPoint> DailyTrend { get; set; } = [];
}
