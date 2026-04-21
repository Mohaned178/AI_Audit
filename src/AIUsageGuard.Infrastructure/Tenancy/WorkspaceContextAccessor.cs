using System.Security.Claims;
using AIUsageGuard.Application.Workspaces.ResolveCurrentWorkspace;
using Microsoft.AspNetCore.Http;

namespace AIUsageGuard.Infrastructure.Tenancy;

public sealed class WorkspaceContextAccessor
{
    private static readonly AsyncLocal<WorkspaceContext?> Current = new();
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ResolveCurrentWorkspaceService _resolveCurrentWorkspaceService;

    public WorkspaceContextAccessor(
        IHttpContextAccessor httpContextAccessor,
        ResolveCurrentWorkspaceService resolveCurrentWorkspaceService)
    {
        _httpContextAccessor = httpContextAccessor;
        _resolveCurrentWorkspaceService = resolveCurrentWorkspaceService;
    }

    public WorkspaceContext Context
    {
        get => Current.Value ??= new WorkspaceContext();
        set => Current.Value = value;
    }

    public Guid? WorkspaceId
    {
        get => Context.WorkspaceId;
        set => Context.WorkspaceId = value;
    }

    public async Task<Guid?> ResolveWorkspaceIdAsync(CancellationToken cancellationToken = default)
    {
        if (Context.WorkspaceId.HasValue)
        {
            return Context.WorkspaceId;
        }

        var rawUserId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out var userId))
        {
            return null;
        }

        var resolved = await _resolveCurrentWorkspaceService.ResolveAsync(userId, Context.WorkspaceId, cancellationToken);
        Context.WorkspaceId = resolved.Workspace.Id;
        return Context.WorkspaceId;
    }
}
