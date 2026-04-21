using AIUsageGuard.Application.Models;
using Microsoft.AspNetCore.Authorization;

namespace AIUsageGuard.Api.Policies;

public sealed class WorkspaceRoleRequirement : IAuthorizationRequirement
{
    public WorkspaceRoleRequirement(WorkspaceRole minimumRole)
    {
        MinimumRole = minimumRole;
    }

    public WorkspaceRole MinimumRole { get; }
}
