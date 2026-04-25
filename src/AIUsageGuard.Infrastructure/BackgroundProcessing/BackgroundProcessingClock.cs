namespace AIUsageGuard.Infrastructure.BackgroundProcessing;

public class BackgroundProcessingClock
{
    public virtual DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
