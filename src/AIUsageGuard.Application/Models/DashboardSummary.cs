namespace AIUsageGuard.Application.Models;

public sealed class DashboardSummary
{
    public Guid WorkspaceId { get; set; }

    public ReportingPeriod Period { get; set; } = new();

    public DashboardTotals Totals { get; set; } = new();

    public UsageSummaryPage TopUsers { get; set; } = new();

    public UsageSummaryPage TopTools { get; set; } = new();

    public AlertsSummary Alerts { get; set; } = new();

    public EstimatedCostSummary Costs { get; set; } = new();
}
