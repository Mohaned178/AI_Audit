using System.Security.Claims;
using AIUsageGuard.Api.Contracts.Notifications;
using AIUsageGuard.Api.Policies;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Notifications.GetNotification;
using AIUsageGuard.Application.Notifications.ListNotifications;
using AIUsageGuard.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIUsageGuard.Api.Controllers;

[ApiController]
[Route("workspaces/{workspaceId:guid}/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly ListNotificationsService _listService;
    private readonly GetNotificationService _getService;
    private readonly WorkspaceContextAccessor _workspaceContextAccessor;

    public NotificationsController(
        ListNotificationsService listService,
        GetNotificationService getService,
        WorkspaceContextAccessor workspaceContextAccessor)
    {
        _listService = listService;
        _getService = getService;
        _workspaceContextAccessor = workspaceContextAccessor;
    }

    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<NotificationListResponse>> List(
        [FromRoute] Guid workspaceId,
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? fromCreatedAt,
        [FromQuery] DateTimeOffset? toCreatedAt,
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

        var result = await _listService.ListAsync(
            new ListNotificationsQuery(
                workspaceId,
                userId.Value,
                ParseNotificationType(type),
                ParseNotificationStatus(status),
                fromCreatedAt,
                toCreatedAt,
                pageNumber,
                pageSize),
            cancellationToken);

        return Ok(new NotificationListResponse(
            result.Items.Select(ToListItemResponse).ToList(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount));
    }

    [HttpGet("{notificationId:guid}")]
    [Authorize(Policy = WorkspacePolicies.WorkspaceAdmin)]
    public async Task<ActionResult<NotificationDetailResponse>> Get(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid notificationId,
        CancellationToken cancellationToken)
    {
        _workspaceContextAccessor.WorkspaceId = workspaceId;
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _getService.GetAsync(
            new GetNotificationQuery(workspaceId, userId.Value, notificationId),
            cancellationToken);

        return Ok(new NotificationDetailResponse(
            result.Notification.Id,
            result.Notification.WorkspaceId,
            ToApiNotificationType(result.Notification.NotificationType),
            result.Notification.Channel,
            ToApiRiskSeverity(result.Notification.Severity),
            ToApiNotificationStatus(result.Notification.Status),
            result.Notification.Subject,
            result.Notification.SummaryBody,
            result.Notification.TriggerFingerprint,
            result.Notification.CoveredPeriodStartUtc,
            result.Notification.CoveredPeriodEndUtc,
            result.Notification.CreatedAtUtc,
            result.DeliveryOutcomes.Select(ToDeliveryResponse).ToList()));
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static NotificationListItemResponse ToListItemResponse(NotificationListItem item)
    {
        return new NotificationListItemResponse(
            item.Notification.Id,
            item.Notification.WorkspaceId,
            ToApiNotificationType(item.Notification.NotificationType),
            item.Notification.Channel,
            ToApiRiskSeverity(item.Notification.Severity),
            ToApiNotificationStatus(item.Notification.Status),
            item.Notification.Subject,
            item.Notification.CoveredPeriodStartUtc,
            item.Notification.CoveredPeriodEndUtc,
            item.Notification.CreatedAtUtc,
            item.RecipientCount,
            item.DeliveredCount,
            item.FailedCount);
    }

    private static NotificationDeliveryOutcomeResponse ToDeliveryResponse(NotificationDeliveryOutcome outcome)
    {
        return new NotificationDeliveryOutcomeResponse(
            outcome.RecipientUserId,
            outcome.RecipientAddress,
            ToApiDeliveryStatus(outcome.DeliveryStatus),
            outcome.AttemptCount,
            outcome.LastAttemptedAtUtc,
            outcome.NextAttemptAtUtc,
            outcome.FinalReason);
    }

    private static NotificationType? ParseNotificationType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "urgent_alert" => NotificationType.UrgentAlert,
            "digest" => NotificationType.Digest,
            _ => throw new RequestFailureException(400, "Notification type is not supported.")
        };
    }

    private static NotificationStatus? ParseNotificationStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "pending" => NotificationStatus.Pending,
            "delivered" => NotificationStatus.Delivered,
            "partially_delivered" => NotificationStatus.PartiallyDelivered,
            "failed" => NotificationStatus.Failed,
            "skipped" => NotificationStatus.Skipped,
            _ => throw new RequestFailureException(400, "Notification status is not supported.")
        };
    }

    private static string ToApiNotificationType(NotificationType value)
    {
        return value switch
        {
            NotificationType.UrgentAlert => "urgent_alert",
            NotificationType.Digest => "digest",
            _ => value.ToString()
        };
    }

    private static string ToApiNotificationStatus(NotificationStatus value)
    {
        return value switch
        {
            NotificationStatus.Pending => "pending",
            NotificationStatus.Delivered => "delivered",
            NotificationStatus.PartiallyDelivered => "partially_delivered",
            NotificationStatus.Failed => "failed",
            NotificationStatus.Skipped => "skipped",
            _ => value.ToString()
        };
    }

    private static string ToApiDeliveryStatus(DeliveryStatus value)
    {
        return value switch
        {
            DeliveryStatus.Pending => "pending",
            DeliveryStatus.RetryScheduled => "retry_scheduled",
            DeliveryStatus.Delivered => "delivered",
            DeliveryStatus.Failed => "failed",
            DeliveryStatus.Skipped => "skipped",
            _ => value.ToString()
        };
    }

    private static string? ToApiRiskSeverity(RiskSeverity? value)
    {
        return value switch
        {
            RiskSeverity.Low => "low",
            RiskSeverity.Medium => "medium",
            RiskSeverity.High => "high",
            null => null,
            _ => value.ToString()
        };
    }
}
