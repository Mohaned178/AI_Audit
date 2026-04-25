namespace AIUsageGuard.Application.Security;

public sealed class SignInHardeningOptions
{
    public const string SectionName = "Security:SignInHardening";

    public int MaxFailedAttempts { get; set; } = 5;

    public TimeSpan FailureWindow { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
}
