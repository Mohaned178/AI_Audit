using System.Diagnostics.Metrics;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Application.Reporting;
using Microsoft.Extensions.Logging;

namespace AIUsageGuard.Application.Reporting.GetDashboard;

public sealed class GetDashboardService
{
    private static readonly Meter ReportingMeter = new("AIUsageGuard.Reporting");
    private static readonly Counter<long> DashboardReadCounter = ReportingMeter.CreateCounter<long>("ai_usage_guard.reporting.dashboard_reads");
    private static readonly Counter<long> EmptyDashboardCounter = ReportingMeter.CreateCounter<long>("ai_usage_guard.reporting.dashboard_empty_results");
    private static readonly Counter<long> InvalidPeriodCounter = ReportingMeter.CreateCounter<long>("ai_usage_guard.reporting.invalid_period_requests");
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly ILogger<GetDashboardService> _logger;

    public GetDashboardService(
        IPlatformStore store,
        IAuditService auditService,
        ILogger<GetDashboardService> logger)
    {
        _store = store;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<GetDashboardResult> GetAsync(GetDashboardQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            Validate(query);

            var period = ReportingPeriodValidator.ValidateAndNormalize(query.Period);
            var totals = await _store.GetDashboardTotalsAsync(query.WorkspaceId, period, cancellationToken);
            var topUsers = await _store.GetUsageSummaryByUserAsync(query.WorkspaceId, period, 1, query.TopCount, cancellationToken);
            var topTools = await _store.GetUsageSummaryByToolAsync(query.WorkspaceId, period, 1, query.TopCount, cancellationToken);
            var alerts = await _store.GetAlertsSummaryAsync(query.WorkspaceId, period, cancellationToken);
            var costs = await _store.GetEstimatedCostSummaryAsync(query.WorkspaceId, period, cancellationToken);
            var summary = new DashboardSummary
            {
                WorkspaceId = query.WorkspaceId,
                Period = period,
                Totals = totals,
                TopUsers = topUsers,
                TopTools = topTools,
                Alerts = alerts,
                Costs = costs
            };

            DashboardReadCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", query.WorkspaceId));
            if (summary.Totals.TotalEvents == 0 && summary.Alerts.TotalFindings == 0)
            {
                EmptyDashboardCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", query.WorkspaceId));
            }

            _logger.LogInformation(
                "Retrieved dashboard summary for workspace {WorkspaceId} from {FromDate} to {ToDate} with {TotalEvents} events, {FlaggedFindingCount} findings, and partial cost {IsPartial}.",
                query.WorkspaceId,
                period.FromDate,
                period.ToDate,
                summary.Totals.TotalEvents,
                summary.Totals.FlaggedFindingCount,
                summary.Costs.IsPartial);

            await RecordAuditAsync(query, "success", "Dashboard summary retrieved.", cancellationToken);
            return new GetDashboardResult(summary);
        }
        catch (RequestFailureException exception)
        {
            if (exception.StatusCode == 400)
            {
                InvalidPeriodCounter.Add(1, new KeyValuePair<string, object?>("workspace.id", query.WorkspaceId));
            }

            _logger.LogWarning(
                exception,
                "Dashboard request failed for workspace {WorkspaceId} from {FromDate} to {ToDate}.",
                query.WorkspaceId,
                query.Period.FromDate,
                query.Period.ToDate);

            await RecordAuditAsync(query, "failed", exception.Message, cancellationToken);
            throw;
        }
    }

    private async Task RecordAuditAsync(
        GetDashboardQuery query,
        string result,
        string reason,
        CancellationToken cancellationToken)
    {
        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = query.WorkspaceId,
            ActorUserId = query.RequestedByUserId,
            ActionType = "dashboard.read",
            TargetType = "dashboard",
            TargetId = query.WorkspaceId.ToString(),
            Result = result,
            Reason = reason
        }, cancellationToken);
    }

    private static void Validate(GetDashboardQuery query)
    {
        if (query.WorkspaceId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Workspace is required.");
        }

        if (query.RequestedByUserId == Guid.Empty)
        {
            throw new RequestFailureException(400, "Requesting user is required.");
        }

        if (query.TopCount < 1 || query.TopCount > 100)
        {
            throw new RequestFailureException(400, "topCount must be between 1 and 100.");
        }
    }
}
