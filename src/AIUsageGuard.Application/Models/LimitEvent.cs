namespace AIUsageGuard.Application.Models;

public sealed class LimitEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; set; }

    public Guid UsageCycleId { get; set; }

    public BillingDimension Dimension { get; set; }

    public LimitEventType EventType { get; set; }

    public string TriggeredBySourceType { get; set; } = string.Empty;

    public string TriggeredBySourceId { get; set; } = string.Empty;

    public decimal CurrentQuantity { get; set; }

    public decimal? ThresholdQuantity { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
