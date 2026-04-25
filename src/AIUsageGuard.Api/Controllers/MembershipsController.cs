using System.Security.Claims;
using AIUsageGuard.Api.Contracts.Memberships;
using AIUsageGuard.Api.Policies;
using AIUsageGuard.Api.Security;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Memberships.CreateMembership;
using AIUsageGuard.Application.Memberships.ListMemberships;
using AIUsageGuard.Application.Memberships.UpdateMembership;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIUsageGuard.Api.Controllers;

[ApiController]
[Route("workspaces/{workspaceId:guid}/memberships")]
[Authorize(Policy = WorkspacePolicies.WorkspaceMember)]
public sealed class MembershipsController : ControllerBase
{
    private readonly ListMembershipsService _listMembershipsService;
    private readonly CreateMembershipService _createMembershipService;
    private readonly UpdateMembershipService _updateMembershipService;
    private readonly WorkspaceContextAccessor _workspaceContextAccessor;
    private readonly IPlatformStore _store;

    public MembershipsController(
        ListMembershipsService listMembershipsService,
        CreateMembershipService createMembershipService,
        UpdateMembershipService updateMembershipService,
        WorkspaceContextAccessor workspaceContextAccessor,
        IPlatformStore store)
    {
        _listMembershipsService = listMembershipsService;
        _createMembershipService = createMembershipService;
        _updateMembershipService = updateMembershipService;
        _workspaceContextAccessor = workspaceContextAccessor;
        _store = store;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MembershipResponse>>> List([FromRoute] Guid workspaceId, CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var memberships = await _listMembershipsService.ListAsync(workspaceId, cancellationToken);
        var response = await Task.WhenAll(memberships.Select(membership => ToResponseAsync(membership)));
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    [RequireProtectedRequestIntegrity]
    public async Task<ActionResult<MembershipResponse>> Create(
        [FromRoute] Guid workspaceId,
        [FromBody] CreateMembershipRequest request,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var actorUserId = GetUserId();
        if (actorUserId is null)
        {
            return Unauthorized();
        }

        var membership = await _createMembershipService.CreateAsync(workspaceId, actorUserId.Value, request.UserEmail, request.Role, cancellationToken);
        return CreatedAtAction(nameof(List), new { workspaceId }, await ToResponseAsync(membership, request.UserEmail));
    }

    [HttpPatch("{membershipId:guid}")]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    [RequireProtectedRequestIntegrity]
    public async Task<ActionResult<MembershipResponse>> Update(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid membershipId,
        [FromBody] UpdateMembershipRequest request,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var actorUserId = GetUserId();
        if (actorUserId is null)
        {
            return Unauthorized();
        }

        var membership = await _updateMembershipService.UpdateAsync(workspaceId, actorUserId.Value, membershipId, request.Role, request.Status, cancellationToken);
        return Ok(await ToResponseAsync(membership));
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private async Task<MembershipResponse> ToResponseAsync(WorkspaceMembership membership, string? userEmail = null)
    {
        var email = userEmail;
        if (string.IsNullOrWhiteSpace(email))
        {
            var user = await _store.FindUserByIdAsync(membership.UserId);
            email = user?.Email ?? string.Empty;
        }

        return new MembershipResponse(
            membership.Id,
            membership.WorkspaceId,
            membership.UserId,
            email,
            membership.Role,
            membership.Status);
    }
}
