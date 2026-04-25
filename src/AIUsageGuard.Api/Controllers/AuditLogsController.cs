using System.Security.Claims;
using AIUsageGuard.Api.Contracts.AuditLogs;
using AIUsageGuard.Api.Policies;
using AIUsageGuard.Application.Auditing.GetAuditLog;
using AIUsageGuard.Application.Auditing.ListAuditLogs;
using AIUsageGuard.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIUsageGuard.Api.Controllers;

[ApiController]
[Route("workspaces/{workspaceId:guid}/audit-logs")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly ListAuditLogsService _listService;
    private readonly GetAuditLogService _getService;
    private readonly WorkspaceContextAccessor _workspaceContextAccessor;

    public AuditLogsController(
        ListAuditLogsService listService,
        GetAuditLogService getService,
        WorkspaceContextAccessor workspaceContextAccessor)
    {
        _listService = listService;
        _getService = getService;
        _workspaceContextAccessor = workspaceContextAccessor;
    }

    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<AuditLogListResponse>> List(
        [FromRoute] Guid workspaceId,
        [FromQuery] DateTimeOffset? fromOccurredAt,
        [FromQuery] DateTimeOffset? toOccurredAt,
        [FromQuery] string? actionType,
        [FromQuery] string? result,
        [FromQuery] Guid? actorUserId,
        [FromQuery] bool? securityRelevant,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var response = await _listService.ListAsync(
            new ListAuditLogsQuery(
                workspaceId,
                userId.Value,
                fromOccurredAt,
                toOccurredAt,
                actionType,
                result,
                actorUserId,
                securityRelevant,
                pageNumber,
                pageSize),
            cancellationToken);

        return Ok(AuditLogResponseFactory.ToAuditLogListResponse(response));
    }

    [HttpGet("{auditLogId:guid}")]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<AuditLogDetailResponse>> Get(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid auditLogId,
        CancellationToken cancellationToken = default)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var response = await _getService.GetAsync(
            new GetAuditLogQuery(workspaceId, userId.Value, auditLogId),
            cancellationToken);

        return Ok(AuditLogResponseFactory.ToAuditLogDetailResponse(response));
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
