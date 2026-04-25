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
        var httpContext = _httpContextAccessor.HttpContext;
        var actionType = ResolveActionType(requirement, httpContext);
        var targetType = ResolveTargetType(requirement, httpContext);
        await _auditService.RecordAsync(new Application.Models.AuditRecord
        {
            WorkspaceId = workspaceId,
            ActorUserId = userId,
            ActionType = actionType,
            TargetType = targetType,
            Result = "denied",
            Reason = reason
        }, cancellationToken);
    }

    private static string ResolveActionType(IAuthorizationRequirement requirement, HttpContext? httpContext)
    {
        if (requirement is WorkspaceMemberRequirement)
        {
            return "ai_usage_event.ingest";
        }

        if (httpContext is null)
        {
            return "authorization.denied";
        }

        return httpContext.Request.Path.Value?.Contains("/risk-policy", StringComparison.OrdinalIgnoreCase) == true
            ? string.Equals(httpContext.Request.Method, HttpMethods.Put, StringComparison.OrdinalIgnoreCase)
                ? "risk_policy.update"
                : "risk_policy.read"
            : httpContext.Request.Path.Value?.Contains("/audit-logs/", StringComparison.OrdinalIgnoreCase) == true
                ? "audit_log.read"
            : httpContext.Request.Path.Value?.Contains("/audit-logs", StringComparison.OrdinalIgnoreCase) == true
                ? "audit_log.list"
            : httpContext.Request.Path.Value?.Contains("/billing/plan-status", StringComparison.OrdinalIgnoreCase) == true
                ? "billing.plan_status.read"
            : httpContext.Request.Path.Value?.Contains("/billing/cycles/", StringComparison.OrdinalIgnoreCase) == true
                ? "billing.cycle.read"
            : httpContext.Request.Path.Value?.Contains("/billing/cycles", StringComparison.OrdinalIgnoreCase) == true
                ? "billing.cycle_history.read"
            : httpContext.Request.Path.Value?.Contains("/notification-preferences", StringComparison.OrdinalIgnoreCase) == true
                ? string.Equals(httpContext.Request.Method, HttpMethods.Put, StringComparison.OrdinalIgnoreCase)
                    ? "notification_preference.update"
                    : "notification_preference.read"
            : httpContext.Request.Path.Value?.Contains("/notifications", StringComparison.OrdinalIgnoreCase) == true
                ? "notification.read"
            : httpContext.Request.Path.Value?.Contains("/dashboard", StringComparison.OrdinalIgnoreCase) == true
                ? "dashboard.read"
            : httpContext.Request.Path.Value?.Contains("/reports/usage-by-user", StringComparison.OrdinalIgnoreCase) == true
                ? "report.usage_by_user.read"
            : httpContext.Request.Path.Value?.Contains("/reports/usage-by-tool", StringComparison.OrdinalIgnoreCase) == true
                ? "report.usage_by_tool.read"
            : httpContext.Request.Path.Value?.Contains("/reports/alerts-summary", StringComparison.OrdinalIgnoreCase) == true
                ? "report.alerts_summary.read"
            : httpContext.Request.Path.Value?.Contains("/reports/cost-summary", StringComparison.OrdinalIgnoreCase) == true
                ? "report.cost_summary.read"
            : httpContext.Request.Path.Value?.Contains("/risk-findings", StringComparison.OrdinalIgnoreCase) == true
                ? "risk_finding.read"
                : httpContext.Request.Path.Value?.Contains("/events", StringComparison.OrdinalIgnoreCase) == true
                    ? "ai_usage_event.history.read"
                    : "authorization.denied";
    }

    private static string ResolveTargetType(IAuthorizationRequirement requirement, HttpContext? httpContext)
    {
        if (requirement is WorkspaceMemberRequirement)
        {
            return "ai_usage_event";
        }

        if (httpContext is null)
        {
            return "authorization";
        }

        return httpContext.Request.Path.Value?.Contains("/risk-policy", StringComparison.OrdinalIgnoreCase) == true
            ? "risk_policy"
            : httpContext.Request.Path.Value?.Contains("/audit-logs/", StringComparison.OrdinalIgnoreCase) == true
                ? "audit_log"
            : httpContext.Request.Path.Value?.Contains("/audit-logs", StringComparison.OrdinalIgnoreCase) == true
                ? "audit_log_history"
            : httpContext.Request.Path.Value?.Contains("/billing/plan-status", StringComparison.OrdinalIgnoreCase) == true
                ? "plan_status"
            : httpContext.Request.Path.Value?.Contains("/billing/cycles/", StringComparison.OrdinalIgnoreCase) == true
                ? "billing_cycle"
            : httpContext.Request.Path.Value?.Contains("/billing/cycles", StringComparison.OrdinalIgnoreCase) == true
                ? "billing_cycle_history"
            : httpContext.Request.Path.Value?.Contains("/notification-preferences", StringComparison.OrdinalIgnoreCase) == true
                ? "notification_preference"
            : httpContext.Request.Path.Value?.Contains("/notifications", StringComparison.OrdinalIgnoreCase) == true
                ? "notification"
            : httpContext.Request.Path.Value?.Contains("/dashboard", StringComparison.OrdinalIgnoreCase) == true
                ? "dashboard"
            : httpContext.Request.Path.Value?.Contains("/reports/usage-by-user", StringComparison.OrdinalIgnoreCase) == true
                ? "usage_summary"
            : httpContext.Request.Path.Value?.Contains("/reports/usage-by-tool", StringComparison.OrdinalIgnoreCase) == true
                ? "usage_summary"
            : httpContext.Request.Path.Value?.Contains("/reports/alerts-summary", StringComparison.OrdinalIgnoreCase) == true
                ? "alerts_summary"
            : httpContext.Request.Path.Value?.Contains("/reports/cost-summary", StringComparison.OrdinalIgnoreCase) == true
                ? "cost_summary"
            : httpContext.Request.Path.Value?.Contains("/risk-findings", StringComparison.OrdinalIgnoreCase) == true
                ? "risk_finding"
                : httpContext.Request.Path.Value?.Contains("/events", StringComparison.OrdinalIgnoreCase) == true
                    ? "event_history"
                    : "authorization";
    }
}
