using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIUsageGuard.Application.BackgroundJobs.RunDeliveryRetry;

public sealed class RunDeliveryRetryService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly Notifications.NotificationProcessingOptions _options;
    private readonly ILogger<RunDeliveryRetryService> _logger;

    public RunDeliveryRetryService(
        IPlatformStore store,
        IAuditService auditService,
        IOptions<Notifications.NotificationProcessingOptions> options,
        ILogger<RunDeliveryRetryService> logger)
    {
        _store = store;
        _auditService = auditService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RunDeliveryRetryResult> RunAsync(
        RunDeliveryRetryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ScheduledForUtc == default)
        {
            throw new RequestFailureException(400, "Scheduled time is required.");
        }

        var jobRun = new BackgroundJobRun
        {
            JobType = BackgroundJobType.DeliveryRetry,
            ScheduledForUtc = command.ScheduledForUtc,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = BackgroundJobRunStatus.Running
        };
        await _store.AddBackgroundJobRunAsync(jobRun, cancellationToken);

        try
        {
            var outcomes = await _store.ListDueNotificationRetryOutcomesAsync(
                command.ScheduledForUtc,
                _options.PendingDeliveryBatchSize,
                cancellationToken);

            foreach (var outcome in outcomes)
            {
                outcome.DeliveryStatus = DeliveryStatus.Pending;
                outcome.NextAttemptAtUtc = null;
                await _store.UpdateNotificationDeliveryOutcomeAsync(outcome, cancellationToken);

                await _auditService.RecordAsync(new AuditRecord
                {
                    ActionType = "notification.retry",
                    TargetType = "notification_delivery_outcome",
                    TargetId = outcome.Id.ToString(),
                    Result = "success",
                    Reason = "Delivery outcome returned to pending for retry."
                }, cancellationToken);
            }

            jobRun.ProcessedItemCount = outcomes.Count;
            jobRun.CompletedAtUtc = DateTimeOffset.UtcNow;
            jobRun.Status = outcomes.Count == 0
                ? BackgroundJobRunStatus.Skipped
                : BackgroundJobRunStatus.Completed;
            await _store.UpdateBackgroundJobRunAsync(jobRun, cancellationToken);

            _logger.LogInformation("Delivery retry scan requeued {RequeuedOutcomeCount} outcomes.", outcomes.Count);
            return new RunDeliveryRetryResult(outcomes.Count);
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
}
