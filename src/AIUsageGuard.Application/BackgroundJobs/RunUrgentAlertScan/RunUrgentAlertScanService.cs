using System.Diagnostics.Metrics;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIUsageGuard.Application.BackgroundJobs.RunUrgentAlertScan;

public sealed class RunUrgentAlertScanService
{
    private static readonly Meter Meter = new("AIUsageGuard.Notifications");
    private static readonly Counter<long> CreatedCounter = Meter.CreateCounter<long>("ai_usage_guard.notifications.urgent.created");
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly NotificationProcessingOptions _options;
    private readonly ILogger<RunUrgentAlertScanService> _logger;

    public RunUrgentAlertScanService(
        IPlatformStore store,
        IAuditService auditService,
        IOptions<NotificationProcessingOptions> options,
        ILogger<RunUrgentAlertScanService> logger)
    {
        _store = store;
        _auditService = auditService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RunUrgentAlertScanResult> RunAsync(
        RunUrgentAlertScanCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ScheduledForUtc == default)
        {
            throw new RequestFailureException(400, "Scheduled time is required.");
        }

        var jobRun = new BackgroundJobRun
        {
            JobType = BackgroundJobType.UrgentAlertScan,
            ScheduledForUtc = command.ScheduledForUtc,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = BackgroundJobRunStatus.Running
        };

        await _store.AddBackgroundJobRunAsync(jobRun, cancellationToken);

        try
        {
            var preferences = await _store.ListNotificationPreferencesAsync(
                urgentAlertsEnabled: true,
                cancellationToken: cancellationToken);

            var created = 0;
            var skipped = 0;

            foreach (var preference in preferences)
            {
                var findings = await _store.ListRiskFindingsAsync(
                    preference.WorkspaceId,
                    null,
                    RiskSeverity.High,
                    RiskFindingStatus.Open,
                    null,
                    null,
                    null,
                    null,
                    1,
                    _options.UrgentAlertBatchSize,
                    cancellationToken);

                foreach (var finding in findings)
                {
                    var candidate = await BuildCandidateAsync(finding, cancellationToken);
                    var existing = await _store.FindNotificationByFingerprintAsync(
                        preference.WorkspaceId,
                        NotificationType.UrgentAlert,
                        candidate.TriggerFingerprint,
                        cancellationToken);

                    if (existing is not null)
                    {
                        skipped++;
                        continue;
                    }

                    var notification = new NotificationMessage
                    {
                        WorkspaceId = preference.WorkspaceId,
                        NotificationType = NotificationType.UrgentAlert,
                        TriggerFingerprint = candidate.TriggerFingerprint,
                        Severity = candidate.Snapshot.Severity,
                        Subject = "High-risk AI activity detected",
                        SummaryBody = BuildSummary(candidate.Snapshot),
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                        CreatedByJobRunId = jobRun.Id,
                        Status = NotificationStatus.Pending
                    };

                    await _store.AddNotificationAsync(notification, cancellationToken);
                    created++;
                    CreatedCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", preference.WorkspaceId));

                    await _auditService.RecordAsync(new AuditRecord
                    {
                        WorkspaceId = preference.WorkspaceId,
                        ActionType = "notification.generate",
                        TargetType = "notification",
                        TargetId = notification.Id.ToString(),
                        Result = "success",
                        Reason = $"Created urgent notification for finding {finding.Id}."
                    }, cancellationToken);
                }
            }

            jobRun.ProcessedItemCount = created + skipped;
            jobRun.CompletedAtUtc = DateTimeOffset.UtcNow;
            jobRun.Status = preferences.Count == 0 && created == 0
                ? BackgroundJobRunStatus.Skipped
                : BackgroundJobRunStatus.Completed;
            await _store.UpdateBackgroundJobRunAsync(jobRun, cancellationToken);

            _logger.LogInformation(
                "Urgent alert scan completed with {ProcessedWorkspaceCount} workspaces, {NotificationsCreated} created, and {NotificationsSkipped} skipped.",
                preferences.Count,
                created,
                skipped);

            return new RunUrgentAlertScanResult(preferences.Count, created, skipped);
        }
        catch (Exception exception)
        {
            jobRun.CompletedAtUtc = DateTimeOffset.UtcNow;
            jobRun.Status = BackgroundJobRunStatus.Failed;
            jobRun.FailureSummary = exception.Message;
            await _store.UpdateBackgroundJobRunAsync(jobRun, cancellationToken);
            _logger.LogError(exception, "Urgent alert scan failed.");
            throw;
        }
    }

    private async Task<UrgentAlertCandidate> BuildCandidateAsync(
        RiskFinding finding,
        CancellationToken cancellationToken)
    {
        var actor = await _store.FindUserByIdAsync(finding.ActorUserId, cancellationToken);
        var snapshot = new UrgentAlertSnapshot
        {
            WorkspaceId = finding.WorkspaceId,
            SourceType = "risk_finding",
            SourceId = finding.Id.ToString(),
            Severity = finding.Severity,
            Reason = finding.Reason,
            ActorDisplayLabel = actor?.DisplayName,
            ToolLabel = finding.ToolName,
            ObservedAtUtc = finding.DetectedAt
        };

        return new UrgentAlertCandidate(
            finding.WorkspaceId,
            finding.Id,
            $"risk-finding:{finding.Id}",
            snapshot);
    }

    private static string BuildSummary(UrgentAlertSnapshot snapshot)
    {
        var actorSegment = string.IsNullOrWhiteSpace(snapshot.ActorDisplayLabel)
            ? string.Empty
            : $" Actor: {snapshot.ActorDisplayLabel}.";
        var toolSegment = string.IsNullOrWhiteSpace(snapshot.ToolLabel)
            ? string.Empty
            : $" Tool: {snapshot.ToolLabel}.";

        return $"A high-severity workspace finding requires review. Reason: {snapshot.Reason}.{actorSegment}{toolSegment} Observed at {snapshot.ObservedAtUtc:O}.";
    }
}
