namespace AIUsageGuard.Application.Models;

public sealed class UsageSummaryRow
{
    public string Dimension { get; set; } = string.Empty;

    public Guid? ActorUserId { get; set; }

    public string DisplayLabel { get; set; } = string.Empty;

    public int TotalEvents { get; set; }

    public int FlaggedFindingCount { get; set; }

    public decimal EstimatedCost { get; set; }

    public int EventsWithEstimatedCost { get; set; }

    public int EventsMissingEstimatedCost { get; set; }

    public DateTimeOffset? LastActivityAt { get; set; }
}
