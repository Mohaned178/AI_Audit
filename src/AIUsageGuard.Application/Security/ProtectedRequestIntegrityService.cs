using System.Diagnostics.Metrics;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIUsageGuard.Application.Security;

public sealed class ProtectedRequestIntegrityService
{
    private static readonly Meter Meter = new("AIUsageGuard.Security");
    private static readonly Counter<long> RejectedCounter = Meter.CreateCounter<long>("ai_usage_guard.protected_request.rejected");

    private readonly IAuditService _auditService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ProtectedRequestIntegrityOptions _options;
    private readonly ILogger<ProtectedRequestIntegrityService> _logger;

    public ProtectedRequestIntegrityService(
        IAuditService auditService,
        IHttpContextAccessor httpContextAccessor,
        IOptions<ProtectedRequestIntegrityOptions> options)
        : this(auditService, httpContextAccessor, options, Microsoft.Extensions.Logging.Abstractions.NullLogger<ProtectedRequestIntegrityService>.Instance)
    {
    }

    public ProtectedRequestIntegrityService(
        IAuditService auditService,
        IHttpContextAccessor httpContextAccessor,
        IOptions<ProtectedRequestIntegrityOptions> options,
        ILogger<ProtectedRequestIntegrityService> logger)
    {
        _auditService = auditService;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        if (!_options.RequireForUnsafeMethods || IsSafeMethod(httpContext.Request.Method))
        {
            return;
        }

        var token = httpContext.Request.Headers[_options.HeaderName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = ResolveWorkspaceId(httpContext),
            ActorUserId = ResolveUserId(httpContext.User),
            ActionType = ResolveActionType(httpContext),
            TargetType = ResolveTargetType(httpContext),
            Result = "denied",
            Reason = "Protected request integrity token is required."
        }, cancellationToken);

        RejectedCounter.Add(
            1,
            new KeyValuePair<string, object?>("workspace.id", ResolveWorkspaceId(httpContext)?.ToString()),
            new KeyValuePair<string, object?>("request.action", ResolveActionType(httpContext)));
        _logger.LogWarning(
            "Rejected protected request {Method} {Path} for workspace {WorkspaceId}: integrity token missing.",
            httpContext.Request.Method,
            httpContext.Request.Path,
            ResolveWorkspaceId(httpContext));

        throw new RequestFailureException(400, "Protected request integrity token is required.");
    }

    private static bool IsSafeMethod(string method)
        => HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method);

    private static Guid? ResolveWorkspaceId(HttpContext httpContext)
    {
        var raw = httpContext.GetRouteValue("workspaceId")?.ToString();
        return Guid.TryParse(raw, out var workspaceId) ? workspaceId : null;
    }

    private static Guid? ResolveUserId(System.Security.Claims.ClaimsPrincipal principal)
    {
        var raw = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    private static string ResolveActionType(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        return path.Contains("/events", StringComparison.OrdinalIgnoreCase)
            ? "ai_usage_event.ingest"
            : path.Contains("/memberships", StringComparison.OrdinalIgnoreCase)
                ? string.Equals(httpContext.Request.Method, HttpMethods.Patch, StringComparison.OrdinalIgnoreCase)
                    ? "membership.update"
                    : "membership.create"
                : path.Contains("/notification-preferences", StringComparison.OrdinalIgnoreCase)
                    ? "notification_preference.update"
                    : path.Contains("/risk-policy", StringComparison.OrdinalIgnoreCase)
                        ? "risk_policy.update"
                        : "protected_request";
    }

    private static string ResolveTargetType(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        return path.Contains("/events", StringComparison.OrdinalIgnoreCase)
            ? "ai_usage_event"
            : path.Contains("/memberships", StringComparison.OrdinalIgnoreCase)
                ? "membership"
                : path.Contains("/notification-preferences", StringComparison.OrdinalIgnoreCase)
                    ? "notification_preference"
                    : path.Contains("/risk-policy", StringComparison.OrdinalIgnoreCase)
                        ? "risk_policy"
                        : "protected_request";
    }
}
