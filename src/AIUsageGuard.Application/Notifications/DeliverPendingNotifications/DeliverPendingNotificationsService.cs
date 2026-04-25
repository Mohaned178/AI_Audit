using System.Diagnostics.Metrics;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIUsageGuard.Application.Notifications.DeliverPendingNotifications;

public sealed class DeliverPendingNotificationsService
{
    private static readonly Meter Meter = new("AIUsageGuard.Notifications");
    private static readonly Counter<long> DeliveredCounter = Meter.CreateCounter<long>("ai_usage_guard.notifications.delivered");
    private static readonly Counter<long> RetryCounter = Meter.CreateCounter<long>("ai_usage_guard.notifications.retries_scheduled");
    private static readonly Counter<long> FailedCounter = Meter.CreateCounter<long>("ai_usage_guard.notifications.failed");
    private readonly IPlatformStore _store;
    private readonly INotificationSender _sender;
    private readonly IAuditService _auditService;
    private readonly NotificationProcessingOptions _options;
    private readonly ILogger<DeliverPendingNotificationsService> _logger;

    public DeliverPendingNotificationsService(
        IPlatformStore store,
        INotificationSender sender,
        IAuditService auditService,
        IOptions<NotificationProcessingOptions> options,
        ILogger<DeliverPendingNotificationsService> logger)
    {
        _store = store;
        _sender = sender;
        _auditService = auditService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DeliverPendingNotificationsResult> RunAsync(
        DeliverPendingNotificationsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ScheduledForUtc == default)
        {
            throw new RequestFailureException(400, "Scheduled time is required.");
        }

        var jobRun = new BackgroundJobRun
        {
            JobType = BackgroundJobType.PendingDelivery,
            ScheduledForUtc = command.ScheduledForUtc,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = BackgroundJobRunStatus.Running
        };
        await _store.AddBackgroundJobRunAsync(jobRun, cancellationToken);

        try
        {
            var deliveredCount = 0;
            var retryCount = 0;
            var failedCount = 0;
            var skippedCount = 0;

            var notifications = await _store.ListNotificationsPendingDeliveryAsync(
                command.ScheduledForUtc,
                _options.PendingDeliveryBatchSize,
                cancellationToken);

            foreach (var notification in notifications)
            {
                var outcomes = (await _store.ListNotificationDeliveryOutcomesAsync(notification.Id, cancellationToken)).ToList();
                if (outcomes.Count == 0)
                {
                    outcomes = await CreatePendingOutcomesAsync(notification, cancellationToken);
                }

                if (outcomes.Count == 0)
                {
                    notification.Status = NotificationStatus.Skipped;
                    await _store.UpdateNotificationAsync(notification, cancellationToken);
                    skippedCount++;

                    await _auditService.RecordAsync(new AuditRecord
                    {
                        WorkspaceId = notification.WorkspaceId,
                        ActionType = "notification.delivery",
                        TargetType = "notification",
                        TargetId = notification.Id.ToString(),
                        Result = "skipped",
                        Reason = "No eligible notification recipients were available."
                    }, cancellationToken);

                    continue;
                }

                foreach (var outcome in outcomes.Where(IsReadyForAttempt))
                {
                    try
                    {
                        await _sender.SendAsync(
                            outcome.RecipientAddress,
                            notification.Subject,
                            notification.SummaryBody,
                            cancellationToken);

                        outcome.AttemptCount++;
                        outcome.LastAttemptedAtUtc = DateTimeOffset.UtcNow;
                        outcome.NextAttemptAtUtc = null;
                        outcome.FinalReason = null;
                        outcome.DeliveryStatus = DeliveryStatus.Delivered;
                        deliveredCount++;
                        DeliveredCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", notification.WorkspaceId));

                        await _auditService.RecordAsync(new AuditRecord
                        {
                            WorkspaceId = notification.WorkspaceId,
                            ActionType = "notification.delivery",
                            TargetType = "notification_delivery_outcome",
                            TargetId = outcome.Id.ToString(),
                            Result = "success",
                            Reason = "Notification delivered."
                        }, cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        outcome.AttemptCount++;
                        outcome.LastAttemptedAtUtc = DateTimeOffset.UtcNow;

                        if (outcome.AttemptCount >= _options.MaxDeliveryAttempts)
                        {
                            outcome.DeliveryStatus = DeliveryStatus.Failed;
                            outcome.NextAttemptAtUtc = null;
                            outcome.FinalReason = exception.Message;
                            failedCount++;
                            FailedCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", notification.WorkspaceId));
                        }
                        else
                        {
                            outcome.DeliveryStatus = DeliveryStatus.RetryScheduled;
                            outcome.NextAttemptAtUtc = CalculateNextAttempt(outcome.AttemptCount, command.ScheduledForUtc);
                            outcome.FinalReason = exception.Message;
                            retryCount++;
                            RetryCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", notification.WorkspaceId));
                        }

                        await _auditService.RecordAsync(new AuditRecord
                        {
                            WorkspaceId = notification.WorkspaceId,
                            ActionType = "notification.delivery",
                            TargetType = "notification_delivery_outcome",
                            TargetId = outcome.Id.ToString(),
                            Result = outcome.DeliveryStatus == DeliveryStatus.Failed ? "failed" : "retry_scheduled",
                            Reason = exception.Message
                        }, cancellationToken);

                        _logger.LogWarning(
                            exception,
                            "Notification {NotificationId} delivery attempt failed for {RecipientAddress}.",
                            notification.Id,
                            outcome.RecipientAddress);
                    }

                    await _store.UpdateNotificationDeliveryOutcomeAsync(outcome, cancellationToken);
                }

                notification.Status = CalculateNotificationStatus(outcomes);
                await _store.UpdateNotificationAsync(notification, cancellationToken);
            }

            jobRun.ProcessedItemCount = notifications.Count;
            jobRun.CompletedAtUtc = DateTimeOffset.UtcNow;
            jobRun.Status = notifications.Count == 0
                ? BackgroundJobRunStatus.Skipped
                : BackgroundJobRunStatus.Completed;
            await _store.UpdateBackgroundJobRunAsync(jobRun, cancellationToken);

            return new DeliverPendingNotificationsResult(
                notifications.Count,
                deliveredCount,
                retryCount,
                failedCount,
                skippedCount);
        }
        catch (Exception exception)
        {
            jobRun.CompletedAtUtc = DateTimeOffset.UtcNow;
            jobRun.Status = BackgroundJobRunStatus.Failed;
            jobRun.FailureSummary = exception.Message;
            await _store.UpdateBackgroundJobRunAsync(jobRun, cancellationToken);
            throw;
        }
    }

    private async Task<List<NotificationDeliveryOutcome>> CreatePendingOutcomesAsync(
        NotificationMessage notification,
        CancellationToken cancellationToken)
    {
        var preference = await _store.FindNotificationPreferenceAsync(notification.WorkspaceId, cancellationToken);
        if (preference is null)
        {
            return [];
        }

        var memberships = await _store.ListMembershipsAsync(notification.WorkspaceId, cancellationToken);
        var eligibleMemberships = memberships
            .Where(item => item.Status == MembershipStatus.Active && item.Role >= WorkspaceRole.Admin)
            .Where(item =>
                preference.RecipientSelectionMode == RecipientSelectionMode.AllAdminsAndOwners ||
                preference.SelectedRecipientUserIds.Contains(item.UserId))
            .ToList();

        var outcomes = new List<NotificationDeliveryOutcome>(eligibleMemberships.Count);
        foreach (var membership in eligibleMemberships)
        {
            var user = await _store.FindUserByIdAsync(membership.UserId, cancellationToken);
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
            {
                continue;
            }

            var outcome = new NotificationDeliveryOutcome
            {
                NotificationId = notification.Id,
                RecipientUserId = membership.UserId,
                RecipientAddress = user.Email,
                DeliveryStatus = DeliveryStatus.Pending
            };

            await _store.AddNotificationDeliveryOutcomeAsync(outcome, cancellationToken);
            outcomes.Add(outcome);
        }

        return outcomes;
    }

    private bool IsReadyForAttempt(NotificationDeliveryOutcome outcome)
    {
        return outcome.DeliveryStatus == DeliveryStatus.Pending &&
               (!outcome.NextAttemptAtUtc.HasValue || outcome.NextAttemptAtUtc <= DateTimeOffset.UtcNow);
    }

    private DateTimeOffset CalculateNextAttempt(int attemptCount, DateTimeOffset scheduledForUtc)
    {
        var multiplier = Math.Pow(2, Math.Max(0, attemptCount - 1));
        var delay = TimeSpan.FromMilliseconds(_options.InitialRetryDelay.TotalMilliseconds * multiplier);
        return scheduledForUtc.Add(delay);
    }

    private static NotificationStatus CalculateNotificationStatus(IReadOnlyCollection<NotificationDeliveryOutcome> outcomes)
    {
        if (outcomes.Count == 0)
        {
            return NotificationStatus.Skipped;
        }

        if (outcomes.All(item => item.DeliveryStatus == DeliveryStatus.Delivered))
        {
            return NotificationStatus.Delivered;
        }

        if (outcomes.Any(item => item.DeliveryStatus == DeliveryStatus.Pending || item.DeliveryStatus == DeliveryStatus.RetryScheduled))
        {
            return outcomes.Any(item => item.DeliveryStatus == DeliveryStatus.Delivered)
                ? NotificationStatus.PartiallyDelivered
                : NotificationStatus.Pending;
        }

        if (outcomes.Any(item => item.DeliveryStatus == DeliveryStatus.Delivered))
        {
            return NotificationStatus.PartiallyDelivered;
        }

        return outcomes.Any(item => item.DeliveryStatus == DeliveryStatus.Failed)
            ? NotificationStatus.Failed
            : NotificationStatus.Skipped;
    }
}
