using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Workspaces;

public sealed record RegisterWorkspaceResult(
    UserAccount User,
    Workspace Workspace,
    WorkspaceMembership Membership);
