namespace AIUsageGuard.Application.Models;

public sealed class UserAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserAccountStatus Status { get; set; } = UserAccountStatus.Active;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastSignInAt { get; set; }

    public int FailedSignInCount { get; set; }

    public DateTimeOffset? LastFailedSignInAt { get; set; }

    public DateTimeOffset? LockedUntilUtc { get; set; }
}
