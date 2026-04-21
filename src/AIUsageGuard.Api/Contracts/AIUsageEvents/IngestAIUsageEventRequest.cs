namespace AIUsageGuard.Api.Contracts.AIUsageEvents;

public sealed record IngestAIUsageEventRequest(
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
    IReadOnlyDictionary<string, string>? Details);
