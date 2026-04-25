namespace AIUsageGuard.Application.Notifications;

public sealed class NotificationProcessingOptions
{
    public const string SectionName = "Notifications:Processing";

    public TimeSpan WorkerInterval { get; set; } = TimeSpan.FromMinutes(1);

    public int PendingDeliveryBatchSize { get; set; } = 100;

    public int UrgentAlertBatchSize { get; set; } = 200;

    public int MaxDeliveryAttempts { get; set; } = 3;

    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMinutes(5);
}
