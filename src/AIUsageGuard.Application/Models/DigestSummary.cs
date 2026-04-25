namespace AIUsageGuard.Application.Models;

public sealed class DigestSummary
{
    public Guid WorkspaceId { get; set; }

    public DateTimeOffset PeriodStartUtc { get; set; }

    public DateTimeOffset PeriodEndUtc { get; set; }

    public int TotalEvents { get; set; }

    public int FlaggedFindingCount { get; set; }

    public decimal EstimatedCostTotal { get; set; }

    public bool IsPartialCost { get; set; }

    public IReadOnlyList<UsageSummaryRow> TopUsers { get; set; } = [];

    public IReadOnlyList<UsageSummaryRow> TopTools { get; set; } = [];
}
