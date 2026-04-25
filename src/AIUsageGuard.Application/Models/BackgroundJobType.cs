namespace AIUsageGuard.Application.Models;

public enum BackgroundJobType
{
    UrgentAlertScan = 0,
    DigestGeneration = 1,
    DeliveryRetry = 2,
    PendingDelivery = 3,
    BillingCycleReconciliation = 4
}
