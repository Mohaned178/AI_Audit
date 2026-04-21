namespace AIUsageGuard.Api.Contracts.Auth;

public sealed record RegisterWorkspaceRequest(
    string Email,
    string Password,
    string DisplayName,
    string WorkspaceName);
