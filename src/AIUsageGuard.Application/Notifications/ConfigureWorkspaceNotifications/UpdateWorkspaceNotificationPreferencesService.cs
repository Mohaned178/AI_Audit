using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Notifications.ConfigureWorkspaceNotifications;

public sealed class UpdateWorkspaceNotificationPreferencesService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;

    public UpdateWorkspaceNotificationPreferencesService(IPlatformStore store, IAuditService auditService)
    {
        _store = store;
        _auditService = auditService;
    }

    public async Task<UpdateWorkspaceNotificationPreferencesResult> UpdateAsync(
        UpdateWorkspaceNotificationPreferencesCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);

        try
        {
            var memberships = await _store.ListMembershipsAsync(command.WorkspaceId, cancellationToken);
            var eligibleUserIds = memberships
                .Where(item => item.Status == MembershipStatus.Active && item.Role >= WorkspaceRole.Admin)
                .Select(item => item.UserId)
                .ToHashSet();

            if (command.RecipientSelectionMode == RecipientSelectionMode.SelectedRecipients)
            {
                if (command.SelectedRecipientUserIds.Count == 0)
                {
                    throw new RequestFailureException(400, "Selected recipients are required when recipientSelectionMode is selected_recipients.");
                }

                if (command.SelectedRecipientUserIds.Any(item => !eligibleUserIds.Contains(item)))
                {
                    throw new RequestFailureException(400, "Selected recipients must be active workspace owners or admins.");
                }
            }

            var preference = await _store.FindNotificationPreferenceAsync(command.WorkspaceId, cancellationToken);
            var created = preference is null;
            preference ??= new NotificationPreference
            {
                WorkspaceId = command.WorkspaceId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            preference.UrgentAlertsEnabled = command.UrgentAlertsEnabled;
            preference.DigestEnabled = command.DigestEnabled;
            preference.DigestCadence = command.DigestCadence;
            preference.RecipientSelectionMode = command.RecipientSelectionMode;
            preference.SelectedRecipientUserIds = command.SelectedRecipientUserIds.Distinct().ToList();
            preference.LastUpdatedAt = DateTimeOffset.UtcNow;
            preference.LastUpdatedByUserId = command.RequestedByUserId;

            if (created)
            {
                await _store.AddNotificationPreferenceAsync(preference, cancellationToken);
            }
            else
            {
                await _store.UpdateNotificationPreferenceAsync(preference, cancellationToken);
            }

            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = command.WorkspaceId,
                ActorUserId = command.RequestedByUserId,
                ActionType = "notification_preference.update",
                TargetType = "notification_preference",
                TargetId = preference.Id.ToString(),
                Result = "success",
                Reason = "Notification preferences updated."
            }, cancellationToken);

            return new UpdateWorkspaceNotificationPreferencesResult(preference, created);
        }
        catch (RequestFailureException exception)
        {
            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = command.WorkspaceId,
                ActorUserId = command.RequestedByUserId,
                ActionType = "notification_preference.update",
                TargetType = "notification_preference",
                Result = "failed",
                Reason = exception.Message
            }, cancellationToken);
            throw;
        }
    }

    private static void Validate(UpdateWorkspaceNotificationPreferencesCommand command)
    {
        if (command.WorkspaceId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Workspace is required.");
        }

        if (command.RequestedByUserId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Requesting user is required.");
        }

        if (command.DigestEnabled && !command.DigestCadence.HasValue)
        {
            throw new RequestFailureException(400, "Digest cadence is required when digest delivery is enabled.");
        }
    }
}
