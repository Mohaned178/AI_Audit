namespace AIUsageGuard.Api.Contracts.RiskDetection;

public sealed record RiskFindingEventContextResponse(
    Guid EventId,
    string EventType,
    Guid ActorUserId,
    string ToolName,
    string? ModelName,
    string? SourceLabel,
    string? PromptPreview,
    string? FileName,
    long? FileSizeBytes,
    decimal? EstimatedCost,
    DateTimeOffset OccurredAt,
    DateTimeOffset ReceivedAt);
