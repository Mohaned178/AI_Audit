using System.Diagnostics.Metrics;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Notifications;
using AIUsageGuard.Application.Reporting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIUsageGuard.Application.BackgroundJobs.RunDigestGeneration;

public sealed class RunDigestGenerationService
{
    private static readonly Meter Meter = new("AIUsageGuard.Notifications");
    private static readonly Counter<long> DigestCounter = Meter.CreateCounter<long>("ai_usage_guard.notifications.digest.created");
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly DigestSchedulingOptions _options;
    private readonly ILogger<RunDigestGenerationService> _logger;

    public RunDigestGenerationService(
        IPlatformStore store,
        IAuditService auditService,
        IOptions<DigestSchedulingOptions> options,
        ILogger<RunDigestGenerationService> logger)
    {
        _store = store;
        _auditService = auditService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RunDigestGenerationResult> RunAsync(
        RunDigestGenerationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ScheduledForUtc == default)
        {
            throw new RequestFailureException(400, "Scheduled time is required.");
        }

        var jobRun = new BackgroundJobRun
        {
            JobType = BackgroundJobType.DigestGeneration,
            ScheduledForUtc = command.ScheduledForUtc,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = BackgroundJobRunStatus.Running
        };
        await _store.AddBackgroundJobRunAsync(jobRun, cancellationToken);

        try
        {
            var preferences = await _store.ListNotificationPreferencesAsync(
                digestEnabled: true,
                cancellationToken: cancellationToken);
            var created = 0;
            var skipped = 0;

            foreach (var preference in preferences)
            {
                if (!TryGetCompletedPeriod(command.ScheduledForUtc, preference.DigestCadence, out var period))
                {
                    skipped++;
                    continue;
                }

                var fingerprint = $"digest:{preference.DigestCadence?.ToString().ToLowerInvariant()}:{period.ToDate:yyyy-MM-dd}";
                var existing = await _store.FindNotificationByFingerprintAsync(
                    preference.WorkspaceId,
                    NotificationType.Digest,
                    fingerprint,
                    cancellationToken);
                if (existing is not null)
                {
                    skipped++;
                    continue;
                }

                var dashboardTotals = await _store.GetDashboardTotalsAsync(preference.WorkspaceId, period, cancellationToken);
                var alertSummary = await _store.GetAlertsSummaryAsync(preference.WorkspaceId, period, cancellationToken);
                var costSummary = await _store.GetEstimatedCostSummaryAsync(preference.WorkspaceId, period, cancellationToken);
                var topUsers = await _store.GetUsageSummaryByUserAsync(preference.WorkspaceId, period, 1, _options.SummaryTopCount, cancellationToken);
                var topTools = await _store.GetUsageSummaryByToolAsync(preference.WorkspaceId, period, 1, _options.SummaryTopCount, cancellationToken);

                var digestSummary = new DigestSummary
                {
                    WorkspaceId = preference.WorkspaceId,
                    PeriodStartUtc = period.NormalizedFromUtc,
                    PeriodEndUtc = period.NormalizedToUtc,
                    TotalEvents = dashboardTotals.TotalEvents,
                    FlaggedFindingCount = alertSummary.TotalFindings,
                    EstimatedCostTotal = costSummary.EstimatedCostTotal,
                    IsPartialCost = costSummary.IsPartial,
                    TopUsers = topUsers.Items,
                    TopTools = topTools.Items
                };

                var notification = new NotificationMessage
                {
                    WorkspaceId = preference.WorkspaceId,
                    NotificationType = NotificationType.Digest,
                    TriggerFingerprint = fingerprint,
                    Subject = $"Workspace activity digest for {period.FromDate:yyyy-MM-dd} to {period.ToDate:yyyy-MM-dd}",
                    SummaryBody = BuildSummary(digestSummary),
                    CoveredPeriodStartUtc = period.NormalizedFromUtc,
                    CoveredPeriodEndUtc = period.NormalizedToUtc,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    CreatedByJobRunId = jobRun.Id,
                    Status = NotificationStatus.Pending
                };

                await _store.AddNotificationAsync(notification, cancellationToken);
                created++;
                DigestCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", preference.WorkspaceId));

                await _auditService.RecordAsync(new AuditRecord
                {
                    WorkspaceId = preference.WorkspaceId,
                    ActionType = "notification.generate",
                    TargetType = "notification",
                    TargetId = notification.Id.ToString(),
                    Result = "success",
                    Reason = "Generated scheduled digest notification."
                }, cancellationToken);
            }

            jobRun.ProcessedItemCount = created + skipped;
            jobRun.CompletedAtUtc = DateTimeOffset.UtcNow;
            jobRun.Status = preferences.Count == 0 && created == 0
                ? BackgroundJobRunStatus.Skipped
                : BackgroundJobRunStatus.Completed;
            await _store.UpdateBackgroundJobRunAsync(jobRun, cancellationToken);

            _logger.LogInformation(
                "Digest generation completed with {ProcessedWorkspaceCount} workspaces, {NotificationsCreated} created, and {NotificationsSkipped} skipped.",
                preferences.Count,
                created,
                skipped);

            return new RunDigestGenerationResult(preferences.Count, created, skipped);
        }
        catch (Exception exception)
        {
            jobRun.CompletedAtUtc = DateTimeOffset.UtcNow;
            jobRun.Status = BackgroundJobRunStatus.Failed;
            jobRun.FailureSummary = exception.Message;
            await _store.UpdateBackgroundJobRunAsync(jobRun, cancellationToken);
            _logger.LogError(exception, "Digest generation failed.");
            throw;
        }
    }

    private bool TryGetCompletedPeriod(
        DateTimeOffset scheduledForUtc,
        DigestCadence? cadence,
        out ReportingPeriod period)
    {
        period = default!;
        if (!cadence.HasValue)
        {
            return false;
        }

        var utcNow = scheduledForUtc.UtcDateTime;
        if (cadence == DigestCadence.Daily)
        {
            var currentDayStart = utcNow.Date;
            if (utcNow - currentDayStart < _options.CompletedPeriodDelay)
            {
                return false;
            }

            var fromDate = DateOnly.FromDateTime(currentDayStart.AddDays(-1));
            period = ReportingPeriodValidator.ValidateAndNormalize(new ReportingPeriodQuery(fromDate, fromDate));
            return true;
        }

        var currentDate = utcNow.Date;
        var offset = ((7 + (currentDate.DayOfWeek - _options.WeeklyDigestStartsOn)) % 7);
        var currentWeekStart = currentDate.AddDays(-offset);
        if (utcNow - currentWeekStart < _options.CompletedPeriodDelay)
        {
            return false;
        }

        var weekStart = currentWeekStart.AddDays(-7);
        var weekEnd = currentWeekStart.AddDays(-1);
        period = ReportingPeriodValidator.ValidateAndNormalize(new ReportingPeriodQuery(
            DateOnly.FromDateTime(weekStart),
            DateOnly.FromDateTime(weekEnd)));
        return true;
    }

    private static string BuildSummary(DigestSummary summary)
    {
        var topUsers = summary.TopUsers.Count == 0
            ? "No user activity recorded."
            : string.Join(", ", summary.TopUsers.Select(item => $"{item.DisplayLabel} ({item.TotalEvents})"));
        var topTools = summary.TopTools.Count == 0
            ? "No tool activity recorded."
            : string.Join(", ", summary.TopTools.Select(item => $"{item.DisplayLabel} ({item.TotalEvents})"));

        return $"Events: {summary.TotalEvents}. Findings: {summary.FlaggedFindingCount}. Estimated cost: {summary.EstimatedCostTotal:0.####}. Partial cost: {summary.IsPartialCost}. Top users: {topUsers}. Top tools: {topTools}.";
    }
}
