using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Api.Contracts.Auth;

public sealed record WorkspaceSessionResponse(
    Guid UserId,
    Guid WorkspaceId,
    string WorkspaceName,
    string Role);
