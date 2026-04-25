namespace AIUsageGuard.Application.Models;

public enum DeliveryStatus
{
    Pending = 0,
    RetryScheduled = 1,
    Delivered = 2,
    Failed = 3,
    Skipped = 4
}
