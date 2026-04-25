using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Billing.ApplyWorkspacePlanAssignment;
using AIUsageGuard.Application.Errors;
using AIUsageGuard.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIUsageGuard.Application.Billing.ReconcileUsageCycles;

public sealed class ReconcileUsageCyclesService
{
    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly ApplyWorkspacePlanAssignmentService _planAssignmentService;
    private readonly BillingDimensionCounter _dimensionCounter;
    private readonly BillingLimitEvaluator _limitEvaluator;
    private readonly BillingReconciliationOptions _options;
    private readonly ILogger<ReconcileUsageCyclesService> _logger;

    public ReconcileUsageCyclesService(
        IPlatformStore store,
        IAuditService auditService,
        ApplyWorkspacePlanAssignmentService planAssignmentService,
        BillingDimensionCounter dimensionCounter,
        BillingLimitEvaluator limitEvaluator,
        IOptions<BillingReconciliationOptions> options,
        ILogger<ReconcileUsageCyclesService> logger)
    {
        _store = store;
        _auditService = auditService;
        _planAssignmentService = planAssignmentService;
        _dimensionCounter = dimensionCounter;
        _limitEvaluator = limitEvaluator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ReconcileUsageCyclesResult> RunAsync(
        ReconcileUsageCyclesCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ScheduledForUtc == default)
        {
            throw new RequestFailureException(400, "Scheduled time is required.");
        }

        var jobRun = new BackgroundJobRun
        {
            JobType = BackgroundJobType.BillingCycleReconciliation,
            ScheduledForUtc = command.ScheduledForUtc,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = BackgroundJobRunStatus.Running
        };

        await _store.AddBackgroundJobRunAsync(jobRun, cancellationToken);

        try
        {
            var dueCycles = await _store.ListUsageCyclesDueForReconciliationAsync(
                command.ScheduledForUtc,
                _options.MaxWorkspaceBatchSize,
                cancellationToken);
            var reconciledCycles = 0;
            var adjustedCycles = 0;
            var adjustmentCount = 0;

            foreach (var cycle in dueCycles)
            {
                var cycleAdjustments = await ReconcileCycleAsync(cycle, jobRun, command.ScheduledForUtc, cancellationToken);
                adjustmentCount += cycleAdjustments;
                if (cycleAdjustments > 0)
                {
                    adjustedCycles++;
                }

                if (cycle.Status is UsageCycleStatus.Closed or UsageCycleStatus.Adjusted)
                {
                    reconciledCycles++;
                }
            }

            jobRun.ProcessedItemCount = reconciledCycles;
            jobRun.CompletedAtUtc = DateTimeOffset.UtcNow;
            jobRun.Status = reconciledCycles == 0
                ? BackgroundJobRunStatus.Skipped
                : BackgroundJobRunStatus.Completed;
            await _store.UpdateBackgroundJobRunAsync(jobRun, cancellationToken);

            await _auditService.RecordAsync(new AuditRecord
            {
                ActionType = "billing.reconcile",
                TargetType = "billing_cycle",
                TargetId = jobRun.Id.ToString(),
                Result = "success",
                Reason = $"Billing reconciliation processed {reconciledCycles} cycles and created {adjustmentCount} adjustments."
            }, cancellationToken);

            _logger.LogInformation(
                "Billing cycle reconciliation ran at {ScheduledForUtc} for {CycleCount} cycles with {AdjustmentCount} adjustments.",
                command.ScheduledForUtc,
                reconciledCycles,
                adjustmentCount);

            return new ReconcileUsageCyclesResult(reconciledCycles, adjustedCycles, adjustmentCount);
        }
        catch (Exception exception)
        {
            jobRun.CompletedAtUtc = DateTimeOffset.UtcNow;
            jobRun.Status = BackgroundJobRunStatus.Failed;
            jobRun.FailureSummary = exception.Message;
            await _store.UpdateBackgroundJobRunAsync(jobRun, cancellationToken);
            _logger.LogError(exception, "Billing cycle reconciliation failed.");
            throw;
        }
    }

    private async Task<int> ReconcileCycleAsync(
        UsageCycle cycle,
        BackgroundJobRun jobRun,
        DateTimeOffset observedAtUtc,
        CancellationToken cancellationToken)
    {
        var metrics = await _store.ListUsageCycleMetricsAsync(cycle.Id, cancellationToken);
        var adjustmentsCreated = 0;

        foreach (var metric in metrics)
        {
            var recomputedQuantity = await _dimensionCounter.CountAsync(
                cycle.WorkspaceId,
                metric.Dimension,
                cycle.CycleStartUtc,
                cycle.CycleEndExclusiveUtc,
                cancellationToken);
            var delta = recomputedQuantity - metric.CurrentQuantity;
            if (delta == 0m)
            {
                continue;
            }

            var adjustment = new CycleAdjustment
            {
                UsageCycleId = cycle.Id,
                Dimension = metric.Dimension,
                AdjustmentType = CycleAdjustmentType.LateActivity,
                DeltaQuantity = delta,
                Reason = $"Late or corrected activity changed {metric.Dimension} by {delta:0.##} during reconciliation.",
                SourceReference = $"reconciliation:{jobRun.Id}",
                RecordedAtUtc = observedAtUtc,
                AppliedByJobRunId = jobRun.Id
            };

            await _store.AddCycleAdjustmentAsync(adjustment, cancellationToken);
            await _limitEvaluator.ApplyAsync(
                cycle.WorkspaceId,
                metric,
                recomputedQuantity,
                observedAtUtc,
                "Reconciliation",
                adjustment.Id.ToString(),
                cancellationToken);
            await _auditService.RecordAsync(new AuditRecord
            {
                WorkspaceId = cycle.WorkspaceId,
                ActionType = "billing.adjustment",
                TargetType = "billing_cycle",
                TargetId = cycle.Id.ToString(),
                Result = "success",
                Reason = adjustment.Reason
            }, cancellationToken);

            adjustmentsCreated++;
        }

        if (cycle.Status == UsageCycleStatus.Open && cycle.CycleEndExclusiveUtc <= observedAtUtc)
        {
            cycle.Status = adjustmentsCreated > 0 ? UsageCycleStatus.Adjusted : UsageCycleStatus.Closed;
            cycle.ClosedAtUtc = observedAtUtc;
        }
        else if (adjustmentsCreated > 0)
        {
            cycle.Status = UsageCycleStatus.Adjusted;
        }

        if (adjustmentsCreated > 0)
        {
            cycle.AdjustmentCount += adjustmentsCreated;
        }

        cycle.LastCalculatedAtUtc = observedAtUtc;
        await _store.UpdateUsageCycleAsync(cycle, cancellationToken);

        if (cycle.CycleEndExclusiveUtc <= observedAtUtc)
        {
            await _planAssignmentService.EnsureCurrentCycleAsync(cycle.WorkspaceId, null, observedAtUtc, cancellationToken);
        }

        return adjustmentsCreated;
    }
}
