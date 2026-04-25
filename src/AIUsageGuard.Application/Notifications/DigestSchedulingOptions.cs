namespace AIUsageGuard.Application.Notifications;

public sealed class DigestSchedulingOptions
{
    public const string SectionName = "Notifications:DigestScheduling";

    public DayOfWeek WeeklyDigestStartsOn { get; set; } = DayOfWeek.Monday;

    public TimeSpan CompletedPeriodDelay { get; set; } = TimeSpan.FromMinutes(5);

    public int SummaryTopCount { get; set; } = 5;
}
