namespace AIUsageGuard.Application.BackgroundJobs.RunDeliveryRetry;

public sealed record RunDeliveryRetryCommand(DateTimeOffset ScheduledForUtc);
