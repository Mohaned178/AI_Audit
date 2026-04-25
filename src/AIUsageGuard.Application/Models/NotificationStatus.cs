namespace AIUsageGuard.Application.Models;

public enum NotificationStatus
{
    Pending = 0,
    Delivered = 1,
    PartiallyDelivered = 2,
    Failed = 3,
    Skipped = 4
}
