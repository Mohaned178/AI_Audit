namespace AIUsageGuard.Domain.AIUsageEvents;

public sealed class AIUsageEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; set; }

    public Guid ActorUserId { get; set; }

    public AIUsageEventType EventType { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string ToolName { get; set; } = string.Empty;

    public string? ModelName { get; set; }

    public string? SourceLabel { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? PromptPreview { get; set; }

    public string? FileName { get; set; }

    public long? FileSizeBytes { get; set; }

    public int? InputTokenCount { get; set; }

    public int? OutputTokenCount { get; set; }

    public decimal? EstimatedCost { get; set; }

    public string? DetailsJson { get; set; }
}
