using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.AIUsageEvents.ListEvents;

public sealed record ListAIUsageEventsResult(
    IReadOnlyList<AIUsageEvent> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
