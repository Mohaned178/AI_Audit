namespace AIUsageGuard.Api.Contracts.Workspaces;

public sealed record WorkspaceContextResponse(
    Guid WorkspaceId,
    string WorkspaceName,
    string CurrentRole);
