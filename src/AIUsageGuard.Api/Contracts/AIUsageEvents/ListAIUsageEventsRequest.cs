namespace AIUsageGuard.Api.Contracts.AIUsageEvents;

public sealed record ListAIUsageEventsRequest(
    string? EventType,
    Guid? ActorUserId,
    string? ToolName,
    DateTimeOffset? FromOccurredAt,
    DateTimeOffset? ToOccurredAt,
    int PageNumber = 1,
    int PageSize = 50);
