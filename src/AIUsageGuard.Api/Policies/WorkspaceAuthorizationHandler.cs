using System.Security.Claims;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
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
    private readonly IAuditService _auditService;

    public WorkspaceAuthorizationHandler(
        IPlatformStore store,
        IHttpContextAccessor httpContextAccessor,
        WorkspaceContextAccessor workspaceContextAccessor,
        IAuditService auditService)
    {
        _store = store;
        _httpContextAccessor = httpContextAccessor;
        _workspaceContextAccessor = workspaceContextAccessor;
        _auditService = auditService;
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
                await RecordDeniedAsync(context.User, null, requirement, "Workspace context could not be resolved.", httpContext.RequestAborted);
                return;
            }
        }

        var userId = GetUserId(context.User);
        if (userId is null)
        {
            await RecordDeniedAsync(context.User, workspaceId, requirement, "Authenticated user is required.", httpContext.RequestAborted);
            return;
        }

        var membership = await _store.FindActiveMembershipAsync(workspaceId.Value, userId.Value, httpContext.RequestAborted);
        if (membership is null)
        {
            await RecordDeniedAsync(context.User, workspaceId, requirement, "User is not a member of the requested workspace.", httpContext.RequestAborted);
            return;
        }

        if (requirement is WorkspaceRoleRequirement roleRequirement && membership.Role < roleRequirement.MinimumRole)
        {
            await RecordDeniedAsync(context.User, workspaceId, requirement, $"User lacks the required {roleRequirement.MinimumRole} role.", httpContext.RequestAborted);
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

    private async Task RecordDeniedAsync(
        ClaimsPrincipal principal,
        Guid? workspaceId,
        IAuthorizationRequirement requirement,
        string reason,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await _auditService.RecordAsync(new Application.Models.AuditRecord
        {
            WorkspaceId = workspaceId,
            ActorUserId = userId,
            ActionType = requirement switch
            {
                WorkspaceRoleRequirement => "ai_usage_event.history.read",
                WorkspaceMemberRequirement => "ai_usage_event.ingest",
                _ => "authorization.denied"
            },
            TargetType = requirement switch
            {
                WorkspaceRoleRequirement => "event_history",
                WorkspaceMemberRequirement => "ai_usage_event",
                _ => "authorization"
            },
            Result = "denied",
            Reason = reason
        }, cancellationToken);
    }
}
