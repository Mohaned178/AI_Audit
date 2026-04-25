using System.Security.Claims;
using AIUsageGuard.Api.Contracts.Notifications;
using AIUsageGuard.Api.Policies;
using AIUsageGuard.Api.Security;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Notifications.ConfigureWorkspaceNotifications;
using AIUsageGuard.Application.Notifications.GetNotificationPreferences;
using AIUsageGuard.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIUsageGuard.Api.Controllers;

[ApiController]
[Route("workspaces/{workspaceId:guid}/notification-preferences")]
public sealed class NotificationPreferencesController : ControllerBase
{
    private readonly GetNotificationPreferencesService _getService;
    private readonly UpdateWorkspaceNotificationPreferencesService _updateService;
    private readonly WorkspaceContextAccessor _workspaceContextAccessor;

    public NotificationPreferencesController(
        GetNotificationPreferencesService getService,
        UpdateWorkspaceNotificationPreferencesService updateService,
        WorkspaceContextAccessor workspaceContextAccessor)
    {
        _getService = getService;
        _updateService = updateService;
        _workspaceContextAccessor = workspaceContextAccessor;
    }

    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<NotificationPreferenceResponse>> Get(
        [FromRoute] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _getService.GetAsync(
            new GetNotificationPreferencesQuery(workspaceId, userId.Value),
            cancellationToken);

        return Ok(ToResponse(result.Preference));
    }

    [HttpPut]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    [RequireProtectedRequestIntegrity]
    public async Task<ActionResult<NotificationPreferenceResponse>> Update(
        [FromRoute] Guid workspaceId,
        [FromBody] UpdateNotificationPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _updateService.UpdateAsync(
            new UpdateWorkspaceNotificationPreferencesCommand(
                workspaceId,
                userId.Value,
                request.UrgentAlertsEnabled,
                request.DigestEnabled,
                ParseDigestCadence(request.DigestCadence),
                ParseRecipientSelectionMode(request.RecipientSelectionMode),
                request.SelectedRecipientUserIds ?? []),
            cancellationToken);

        return Ok(ToResponse(result.Preference));
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static NotificationPreferenceResponse ToResponse(NotificationPreference preference)
    {
        return new NotificationPreferenceResponse(
            preference.WorkspaceId,
            preference.UrgentAlertsEnabled,
            preference.DigestEnabled,
            ToApiDigestCadence(preference.DigestCadence),
            ToApiRecipientSelectionMode(preference.RecipientSelectionMode),
            preference.SelectedRecipientUserIds,
            preference.CreatedAt,
            preference.LastUpdatedAt,
            preference.LastUpdatedByUserId);
    }

    private static DigestCadence? ParseDigestCadence(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "daily" => DigestCadence.Daily,
            "weekly" => DigestCadence.Weekly,
            _ => throw new RequestFailureException(400, "Digest cadence is not supported.")
        };
    }

    private static RecipientSelectionMode ParseRecipientSelectionMode(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "all_admins_and_owners" => RecipientSelectionMode.AllAdminsAndOwners,
            "selected_recipients" => RecipientSelectionMode.SelectedRecipients,
            _ => throw new RequestFailureException(400, "Recipient selection mode is not supported.")
        };
    }

    private static string? ToApiDigestCadence(DigestCadence? cadence)
    {
        return cadence switch
        {
            DigestCadence.Daily => "daily",
            DigestCadence.Weekly => "weekly",
            null => null,
            _ => cadence.ToString()
        };
    }

    private static string ToApiRecipientSelectionMode(RecipientSelectionMode mode)
    {
        return mode switch
        {
            RecipientSelectionMode.AllAdminsAndOwners => "all_admins_and_owners",
            RecipientSelectionMode.SelectedRecipients => "selected_recipients",
            _ => mode.ToString()
        };
    }
}
