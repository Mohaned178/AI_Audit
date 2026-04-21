using System.Security.Claims;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AIUsageGuard.Api.Policies;

public sealed class WorkspaceAuthorizationHandler : AuthorizationHandler<IAuthorizationRequirement>
{
    private readonly IPlatformStore _store;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly WorkspaceContextAccessor _workspaceContextAccessor;

    public WorkspaceAuthorizationHandler(
        IPlatformStore store,
        IHttpContextAccessor httpContextAccessor,
        WorkspaceContextAccessor workspaceContextAccessor)
    {
        _store = store;
        _httpContextAccessor = httpContextAccessor;
        _workspaceContextAccessor = workspaceContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IAuthorizationRequirement requirement)
    {
        if (requirement is not WorkspaceMemberRequirement and not WorkspaceRoleRequirement)
        {
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        var workspaceId = ResolveWorkspaceId(httpContext);
        if (workspaceId is null)
        {
            workspaceId = await _workspaceContextAccessor.ResolveWorkspaceIdAsync(httpContext.RequestAborted);
            if (workspaceId is null)
            {
                return;
            }
        }

        var userId = GetUserId(context.User);
        if (userId is null)
        {
            return;
        }

        var membership = await _store.FindActiveMembershipAsync(workspaceId.Value, userId.Value, httpContext.RequestAborted);
        if (membership is null)
        {
            return;
        }

        if (requirement is WorkspaceRoleRequirement roleRequirement && membership.Role < roleRequirement.MinimumRole)
        {
            return;
        }

        _workspaceContextAccessor.WorkspaceId = workspaceId;
        context.Succeed(requirement);
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static Guid? ResolveWorkspaceId(HttpContext httpContext)
    {
        var raw = httpContext.GetRouteValue("workspaceId")?.ToString();
        return Guid.TryParse(raw, out var workspaceId) ? workspaceId : null;
    }
}
