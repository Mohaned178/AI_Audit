using System.Security.Claims;
using AIUsageGuard.Api.Contracts.Billing;
using AIUsageGuard.Api.Policies;
using AIUsageGuard.Application.Billing.GetBillingCycle;
using AIUsageGuard.Application.Billing.GetPlanStatus;
using AIUsageGuard.Application.Billing.ListBillingCycles;
using AIUsageGuard.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIUsageGuard.Api.Controllers;

[ApiController]
[Route("workspaces/{workspaceId:guid}/billing")]
public sealed class BillingController : ControllerBase
{
    private readonly GetPlanStatusService _getPlanStatusService;
    private readonly ListBillingCyclesService _listBillingCyclesService;
    private readonly GetBillingCycleService _getBillingCycleService;
    private readonly WorkspaceContextAccessor _workspaceContextAccessor;

    public BillingController(
        GetPlanStatusService getPlanStatusService,
        ListBillingCyclesService listBillingCyclesService,
        GetBillingCycleService getBillingCycleService,
        WorkspaceContextAccessor workspaceContextAccessor)
    {
        _getPlanStatusService = getPlanStatusService;
        _listBillingCyclesService = listBillingCyclesService;
        _getBillingCycleService = getBillingCycleService;
        _workspaceContextAccessor = workspaceContextAccessor;
    }

    [HttpGet("plan-status")]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<PlanStatusResponse>> GetPlanStatus(
        [FromRoute] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _getPlanStatusService.GetAsync(
            new GetPlanStatusQuery(workspaceId, userId.Value),
            cancellationToken);

        return Ok(BillingResponseFactory.ToPlanStatusResponse(result.Snapshot, result.CurrentCycle, result.AssignedFromCycleStartUtc));
    }

    [HttpGet("cycles")]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<BillingCycleListResponse>> ListBillingCycles(
        [FromRoute] Guid workspaceId,
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

        var result = await _listBillingCyclesService.ListAsync(
            new ListBillingCyclesQuery(workspaceId, userId.Value, pageNumber, pageSize),
            cancellationToken);

        return Ok(BillingResponseFactory.ToBillingCycleListResponse(result));
    }

    [HttpGet("cycles/{cycleId:guid}")]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<BillingCycleDetailResponse>> GetBillingCycle(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid cycleId,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _getBillingCycleService.GetAsync(
            new GetBillingCycleQuery(workspaceId, userId.Value, cycleId),
            cancellationToken);

        return Ok(BillingResponseFactory.ToBillingCycleDetailResponse(result));
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
