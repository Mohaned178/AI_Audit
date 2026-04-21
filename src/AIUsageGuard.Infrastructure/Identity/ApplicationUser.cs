using AIUsageGuard.Application.Models;
using Microsoft.AspNetCore.Identity;

namespace AIUsageGuard.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public UserAccountStatus Status { get; set; } = UserAccountStatus.Active;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastSignInAt { get; set; }
}
