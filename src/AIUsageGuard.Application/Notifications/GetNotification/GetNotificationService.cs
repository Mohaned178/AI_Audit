using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;

namespace AIUsageGuard.Application.Notifications.GetNotification;

public sealed class GetNotificationService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;

    public GetNotificationService(IPlatformStore store, IAuditService auditService)
    {
        _store = store;
        _auditService = auditService;
    }

    public async Task<GetNotificationResult> GetAsync(
        GetNotificationQuery query,
        CancellationToken cancellationToken = default)
    {
        Validate(query);

        try
        {
            var notification = await _store.FindNotificationAsync(query.WorkspaceId, query.NotificationId, cancellationToken)
                ?? throw new RequestFailureException(404, "Notification not found.");
            var outcomes = await _store.ListNotificationDeliveryOutcomesAsync(notification.Id, cancellationToken);

            await _auditService.RecordAsync(new Models.AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "notification.read",
                TargetType = "notification",
                TargetId = notification.Id.ToString(),
                Result = "success",
                Reason = "Notification retrieved."
            }, cancellationToken);

            return new GetNotificationResult(notification, outcomes);
        }
        catch (RequestFailureException exception)
        {
            await _auditService.RecordAsync(new Models.AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "notification.read",
                TargetType = "notification",
                TargetId = query.NotificationId.ToString(),
                Result = "failed",
                Reason = exception.Message
            }, cancellationToken);
            throw;
        }
    }

    private static void Validate(GetNotificationQuery query)
    {
        if (query.WorkspaceId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Workspace is required.");
        }

        if (query.RequestedByUserId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Requesting user is required.");
        }

        if (query.NotificationId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Notification is required.");
        }
    }
}
