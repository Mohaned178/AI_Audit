using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Reporting;

namespace AIUsageGuard.Application.Notifications.ListNotifications;

public sealed class ListNotificationsService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;

    public ListNotificationsService(IPlatformStore store, IAuditService auditService)
    {
        _store = store;
        _auditService = auditService;
    }

    public async Task<ListNotificationsResult> ListAsync(
        ListNotificationsQuery query,
        CancellationToken cancellationToken = default)
    {
        Validate(query);

        try
        {
            var totalCount = await _store.CountNotificationsAsync(
                query.WorkspaceId,
                query.NotificationType,
                query.Status,
                query.FromCreatedAt,
                query.ToCreatedAt,
                cancellationToken);

            var notifications = await _store.ListNotificationsAsync(
                query.WorkspaceId,
                query.NotificationType,
                query.Status,
                query.FromCreatedAt,
                query.ToCreatedAt,
                query.PageNumber,
                query.PageSize,
                cancellationToken);

            var items = new List<NotificationListItem>(notifications.Count);
            foreach (var notification in notifications)
            {
                var outcomes = await _store.ListNotificationDeliveryOutcomesAsync(notification.Id, cancellationToken);
                items.Add(new NotificationListItem(
                    notification,
                    outcomes.Count,
                    outcomes.Count(item => item.DeliveryStatus == DeliveryStatus.Delivered),
                    outcomes.Count(item => item.DeliveryStatus == DeliveryStatus.Failed)));
            }

            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "notification.read",
                TargetType = "notification_list",
                Result = "success",
                Reason = "Notification history retrieved."
            }, cancellationToken);

            return new ListNotificationsResult(items, query.PageNumber, query.PageSize, totalCount);
        }
        catch (RequestFailureException exception)
        {
            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "notification.read",
                TargetType = "notification_list",
                Result = "failed",
                Reason = exception.Message
            }, cancellationToken);
            throw;
        }
    }

    private static void Validate(ListNotificationsQuery query)
    {
        if (query.WorkspaceId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Workspace is required.");
        }

        if (query.RequestedByUserId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Requesting user is required.");
        }

        if (query.FromCreatedAt.HasValue && query.ToCreatedAt.HasValue && query.FromCreatedAt > query.ToCreatedAt)
        {
            throw new RequestFailureException(400, "The start date must be earlier than or equal to the end date.");
        }

        ReportingPeriodValidator.ValidatePage(query.PageNumber, query.PageSize);
    }
}
