namespace AIUsageGuard.Api.Contracts.Billing;

public sealed record CycleAdjustmentResponse(
    Guid AdjustmentId,
    DateTimeOffset RecordedAtUtc,
    string Dimension,
    decimal DeltaQuantity,
    string AdjustmentType,
    string SourceReference,
    string Reason);
