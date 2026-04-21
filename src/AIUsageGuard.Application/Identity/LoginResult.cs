using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Identity;

public sealed record LoginResult(
    UserAccount User,
    Workspace Workspace,
    WorkspaceMembership Membership);
