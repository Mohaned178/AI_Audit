using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.AIUsageEvents.IngestEvent;

public sealed record IngestAIUsageEventResult(
    AIUsageEvent Event,
    bool IsDuplicate);
