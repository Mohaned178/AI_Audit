using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.AIUsageEvents.IngestEvent;

public sealed record IngestAIUsageEventCommand(
    Guid WorkspaceId,
    Guid ActorUserId,
    string IdempotencyKey,
    string EventType,
    DateTimeOffset OccurredAt,
    string ToolName,
    string? ModelName,
    string? SourceLabel,
    string? PromptPreview,
    string? FileName,
    long? FileSizeBytes,
    int? InputTokenCount,
    int? OutputTokenCount,
    decimal? EstimatedCost,
    string? DetailsJson);
