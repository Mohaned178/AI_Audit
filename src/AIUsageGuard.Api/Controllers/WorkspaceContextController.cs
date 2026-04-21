using System.Security.Claims;
using AIUsageGuard.Api.Contracts.Workspaces;
using AIUsageGuard.Application.Workspaces.GetWorkspaceContext;
using AIUsageGuard.Api.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIUsageGuard.Api.Controllers;

[ApiController]
[Route("workspaces/{workspaceId:guid}/context")]
[Authorize(Policy = WorkspacePolicies.WorkspaceMember)]
public sealed class WorkspaceContextController : ControllerBase
{
    private readonly GetWorkspaceContextService _getWorkspaceContextService;

    public WorkspaceContextController(GetWorkspaceContextService getWorkspaceContextService)
    {
        _getWorkspaceContextService = getWorkspaceContextService;
    }

    [HttpGet]
    public async Task<ActionResult<WorkspaceContextResponse>> Get([FromRoute] Guid workspaceId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _getWorkspaceContextService.GetAsync(workspaceId, userId.Value, cancellationToken);
        return Ok(new WorkspaceContextResponse(result.Workspace.Id, result.Workspace.Name, result.Membership.Role.ToString()));
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
