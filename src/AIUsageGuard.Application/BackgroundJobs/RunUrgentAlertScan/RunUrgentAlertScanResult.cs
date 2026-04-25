namespace AIUsageGuard.Application.BackgroundJobs.RunUrgentAlertScan;

public sealed record RunUrgentAlertScanResult(
    int ProcessedWorkspaceCount,
    int NotificationsCreated,
    int NotificationsSkipped);
