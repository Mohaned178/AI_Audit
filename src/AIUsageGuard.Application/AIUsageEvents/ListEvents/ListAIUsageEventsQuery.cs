using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.AIUsageEvents.ListEvents;

public sealed record ListAIUsageEventsQuery(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    AIUsageEventType? EventType,
    Guid? ActorUserId,
    string? ToolName,
    DateTimeOffset? FromOccurredAt,
    DateTimeOffset? ToOccurredAt,
    int PageNumber,
    int PageSize);
