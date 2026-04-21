using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Workspaces;

public sealed record WorkspaceContextResult(
    Workspace Workspace,
    WorkspaceMembership Membership);
