using System.Security.Claims;
using AIUsageGuard.Api.Contracts.Reporting;
using AIUsageGuard.Api.Policies;
using AIUsageGuard.Application.Reporting;
using AIUsageGuard.Application.Reporting.GetDashboard;
using AIUsageGuard.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIUsageGuard.Api.Controllers;

[ApiController]
[Route("workspaces/{workspaceId:guid}/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly GetDashboardService _service;
    private readonly WorkspaceContextAccessor _workspaceContextAccessor;

    public DashboardController(
        GetDashboardService service,
        WorkspaceContextAccessor workspaceContextAccessor)
    {
        _service = service;
        _workspaceContextAccessor = workspaceContextAccessor;
    }

    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<DashboardResponse>> Get(
        [FromRoute] Guid workspaceId,
        [FromQuery] ReportingPeriodRequest request,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _service.GetAsync(
            new GetDashboardQuery(
                workspaceId,
                userId.Value,
                new ReportingPeriodQuery(request.FromDate, request.ToDate)),
            cancellationToken);

        return Ok(ReportingResponseFactory.ToDashboardResponse(result.Summary));
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
