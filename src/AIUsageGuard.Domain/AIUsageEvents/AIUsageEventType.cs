namespace AIUsageGuard.Domain.AIUsageEvents;

public enum AIUsageEventType
{
    PromptSubmitted,
    FileUploaded,
    ToolUsed,
    UsageRecorded,
    ModelCalled
}
