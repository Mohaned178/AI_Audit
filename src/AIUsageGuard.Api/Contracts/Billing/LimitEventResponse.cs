namespace AIUsageGuard.Api.Contracts.Billing;

public sealed record LimitEventResponse(
    Guid EventId,
    string EventType,
    string Dimension,
    DateTimeOffset OccurredAtUtc,
    decimal CurrentQuantity,
    decimal? ThresholdQuantity,
    string Reason);
