namespace AIUsageGuard.Application.BackgroundJobs.RunDigestGeneration;

public sealed record RunDigestGenerationResult(
    int ProcessedWorkspaceCount,
    int NotificationsCreated,
    int NotificationsSkipped);
