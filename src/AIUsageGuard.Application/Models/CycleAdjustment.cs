namespace AIUsageGuard.Application.Models;

public sealed class CycleAdjustment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UsageCycleId { get; set; }

    public BillingDimension Dimension { get; set; }

    public CycleAdjustmentType AdjustmentType { get; set; }

    public decimal DeltaQuantity { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string SourceReference { get; set; } = string.Empty;

    public DateTimeOffset RecordedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Guid? AppliedByJobRunId { get; set; }
}
