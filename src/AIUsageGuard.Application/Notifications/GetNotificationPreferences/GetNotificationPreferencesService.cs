using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Notifications.GetNotificationPreferences;

public sealed class GetNotificationPreferencesService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;

    public GetNotificationPreferencesService(IPlatformStore store, IAuditService auditService)
    {
        _store = store;
        _auditService = auditService;
    }

    public async Task<GetNotificationPreferencesResult> GetAsync(
        GetNotificationPreferencesQuery query,
        CancellationToken cancellationToken = default)
    {
        Validate(query);

        try
        {
            var preference = await _store.FindNotificationPreferenceAsync(query.WorkspaceId, cancellationToken);
            if (preference is null)
            {
                preference = new NotificationPreference
                {
                    WorkspaceId = query.WorkspaceId,
                    RecipientSelectionMode = RecipientSelectionMode.AllAdminsAndOwners,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastUpdatedAt = DateTimeOffset.UtcNow,
                    LastUpdatedByUserId = query.RequestedByUserId
                };

                await _store.AddNotificationPreferenceAsync(preference, cancellationToken);
            }

            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "notification_preference.read",
                TargetType = "notification_preference",
                TargetId = preference.Id.ToString(),
                Result = "success",
                Reason = "Notification preferences retrieved."
            }, cancellationToken);

            return new GetNotificationPreferencesResult(preference);
        }
        catch (RequestFailureException exception)
        {
            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = query.WorkspaceId,
                ActorUserId = query.RequestedByUserId,
                ActionType = "notification_preference.read",
                TargetType = "notification_preference",
                Result = "failed",
                Reason = exception.Message
            }, cancellationToken);
            throw;
        }
    }

    private static void Validate(GetNotificationPreferencesQuery query)
    {
        if (query.WorkspaceId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Workspace is required.");
        }

        if (query.RequestedByUserId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Requesting user is required.");
        }
    }
}
