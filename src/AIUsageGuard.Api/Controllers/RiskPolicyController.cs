using System.Security.Claims;
using AIUsageGuard.Api.Contracts.RiskDetection;
using AIUsageGuard.Api.Policies;
using AIUsageGuard.Api.Security;
using AIUsageGuard.Application.RiskDetection.GetRiskPolicy;
using AIUsageGuard.Application.RiskDetection.UpdateRiskPolicy;
using AIUsageGuard.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIUsageGuard.Api.Controllers;

[ApiController]
[Route("workspaces/{workspaceId:guid}/risk-policy")]
public sealed class RiskPolicyController : ControllerBase
{
    private readonly GetWorkspaceRiskPolicyService _getService;
    private readonly UpdateWorkspaceRiskPolicyService _updateService;
    private readonly WorkspaceContextAccessor _workspaceContextAccessor;

    public RiskPolicyController(
        GetWorkspaceRiskPolicyService getService,
        UpdateWorkspaceRiskPolicyService updateService,
        WorkspaceContextAccessor workspaceContextAccessor)
    {
        _getService = getService;
        _updateService = updateService;
        _workspaceContextAccessor = workspaceContextAccessor;
    }

    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<WorkspaceRiskPolicyResponse>> Get(
        [FromRoute] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _getService.GetAsync(new GetWorkspaceRiskPolicyQuery(workspaceId, userId.Value), cancellationToken);
        return Ok(ToResponse(result.Policy));
    }

    [HttpPut]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    [RequireProtectedRequestIntegrity]
    public async Task<ActionResult<WorkspaceRiskPolicyResponse>> Update(
        [FromRoute] Guid workspaceId,
        [FromBody] UpdateWorkspaceRiskPolicyRequest request,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _updateService.UpdateAsync(
            new UpdateWorkspaceRiskPolicyCommand(
                workspaceId,
                userId.Value,
                request.ApprovedTools,
                request.PerEventEstimatedCostThreshold,
                request.DailyEstimatedCostThreshold),
            cancellationToken);

        return Ok(ToResponse(result.Policy));
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static WorkspaceRiskPolicyResponse ToResponse(AIUsageGuard.Application.Models.WorkspaceRiskPolicy policy)
    {
        return new WorkspaceRiskPolicyResponse(
            policy.WorkspaceId,
            policy.ApprovedTools,
            policy.PerEventEstimatedCostThreshold,
            policy.DailyEstimatedCostThreshold,
            policy.CreatedAt,
            policy.LastUpdatedAt,
            policy.LastUpdatedByUserId);
    }
}
