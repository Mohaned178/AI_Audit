namespace AIUsageGuard.Api.Contracts.AIUsageEvents;

public sealed record AIUsageEventHistoryItemResponse(
    Guid EventId,
    Guid WorkspaceId,
    Guid ActorUserId,
    string EventType,
    string IdempotencyKey,
    string ToolName,
    string? ModelName,
    string? SourceLabel,
    string? PromptPreview,
    string? FileName,
    long? FileSizeBytes,
    int? InputTokenCount,
    int? OutputTokenCount,
    decimal? EstimatedCost,
    DateTimeOffset OccurredAt,
    DateTimeOffset ReceivedAt,
    IReadOnlyDictionary<string, string>? Details);
