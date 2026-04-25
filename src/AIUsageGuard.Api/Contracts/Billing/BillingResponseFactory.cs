using AIUsageGuard.Application.Billing.GetBillingCycle;
using AIUsageGuard.Application.Billing.ListBillingCycles;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Api.Contracts.Billing;

internal static class BillingResponseFactory
{
    public static PlanStatusResponse ToPlanStatusResponse(
        PlanStatusSnapshot snapshot,
        UsageCycle cycle,
        DateTimeOffset assignedFromCycleStartUtc)
    {
        return new PlanStatusResponse(
            snapshot.WorkspaceId,
            new PlanAssignmentSummaryResponse(
                snapshot.PlanCode,
                snapshot.PlanDisplayName,
                assignedFromCycleStartUtc,
                snapshot.NextPlanCode,
                snapshot.NextPlanStartsAtUtc),
            new CurrentCycleSummaryResponse(
                cycle.Id,
                cycle.CycleStartUtc,
                cycle.CycleEndExclusiveUtc,
                ToApiUsageCycleStatus(cycle.Status)),
            snapshot.MetricStatuses.Select(ToPlanMetricStatusResponse).ToList());
    }

    public static BillingCycleListResponse ToBillingCycleListResponse(ListBillingCyclesResult result)
    {
        return new BillingCycleListResponse(
            result.WorkspaceId,
            new BillingCyclePageResponse(
                result.Items.Select(ToBillingCycleSummaryResponse).ToList(),
                result.PageNumber,
                result.PageSize,
                result.TotalCount));
    }

    public static BillingCycleDetailResponse ToBillingCycleDetailResponse(GetBillingCycleResult result)
    {
        return new BillingCycleDetailResponse(
            result.WorkspaceId,
            ToBillingCycleSummaryResponse(result.Cycle),
            result.LimitEvents.Select(ToLimitEventResponse).ToList(),
            result.Adjustments.Select(ToCycleAdjustmentResponse).ToList());
    }

    public static PlanMetricStatusResponse ToPlanMetricStatusResponse(UsageCycleMetric metric)
        => new(
            ToApiBillingDimension(metric.Dimension),
            metric.IncludedQuantity,
            metric.WarningThresholdQuantity,
            metric.HardLimitQuantity,
            metric.CurrentQuantity,
            Math.Max(0m, metric.IncludedQuantity - metric.CurrentQuantity),
            metric.OverageQuantity,
            ToApiBillingLimitBehavior(metric.LimitBehavior),
            ToApiUsageCycleMetricState(metric.State));

    public static BillingCycleSummaryResponse ToBillingCycleSummaryResponse(BillingCycleSummary summary)
        => new(
            summary.UsageCycleId,
            summary.CycleStartUtc,
            summary.CycleEndExclusiveUtc,
            ToApiUsageCycleStatus(summary.Status),
            summary.PlanCode,
            summary.PlanDisplayName,
            summary.Metrics.Select(ToBillingCycleMetricResponse).ToList(),
            summary.WarningEventCount,
            summary.RestrictionEventCount,
            summary.AdjustmentCount);

    public static BillingCycleMetricResponse ToBillingCycleMetricResponse(UsageCycleMetric metric)
        => new(
            ToApiBillingDimension(metric.Dimension),
            metric.IncludedQuantity,
            metric.CurrentQuantity,
            metric.OverageQuantity,
            ToApiUsageCycleMetricState(metric.State),
            ToApiBillingLimitBehavior(metric.LimitBehavior));

    public static LimitEventResponse ToLimitEventResponse(LimitEvent limitEvent)
        => new(
            limitEvent.Id,
            ToApiLimitEventType(limitEvent.EventType),
            ToApiBillingDimension(limitEvent.Dimension),
            limitEvent.OccurredAtUtc,
            limitEvent.CurrentQuantity,
            limitEvent.ThresholdQuantity,
            limitEvent.Reason);

    public static CycleAdjustmentResponse ToCycleAdjustmentResponse(CycleAdjustment adjustment)
        => new(
            adjustment.Id,
            adjustment.RecordedAtUtc,
            ToApiBillingDimension(adjustment.Dimension),
            adjustment.DeltaQuantity,
            ToApiCycleAdjustmentType(adjustment.AdjustmentType),
            adjustment.SourceReference,
            adjustment.Reason);

    public static string ToApiBillingDimension(BillingDimension dimension)
        => dimension switch
        {
            BillingDimension.ActiveMembers => "active_members",
            BillingDimension.AIActivityEvents => "ai_activity_events",
            BillingDimension.EstimatedCost => "estimated_cost",
            _ => dimension.ToString()
        };

    public static string ToApiBillingLimitBehavior(BillingLimitBehavior behavior)
        => behavior switch
        {
            BillingLimitBehavior.WarnOnly => "warn_only",
            BillingLimitBehavior.Restrict => "restrict",
            BillingLimitBehavior.AllowOverage => "allow_overage",
            _ => behavior.ToString()
        };

    public static string ToApiUsageCycleStatus(UsageCycleStatus status)
        => status switch
        {
            UsageCycleStatus.Open => "open",
            UsageCycleStatus.Closed => "closed",
            UsageCycleStatus.Adjusted => "adjusted",
            _ => status.ToString()
        };

    public static string ToApiUsageCycleMetricState(UsageCycleMetricState state)
        => state switch
        {
            UsageCycleMetricState.WithinLimit => "within_limit",
            UsageCycleMetricState.Warning => "warning",
            UsageCycleMetricState.Overage => "overage",
            UsageCycleMetricState.Restricted => "restricted",
            _ => state.ToString()
        };

    public static string ToApiLimitEventType(LimitEventType eventType)
        => eventType switch
        {
            LimitEventType.WarningRaised => "warning_raised",
            LimitEventType.OverageStarted => "overage_started",
            LimitEventType.RestrictionApplied => "restriction_applied",
            LimitEventType.StateReturnedToWithinLimit => "state_returned_to_within_limit",
            _ => eventType.ToString()
        };

    public static string ToApiCycleAdjustmentType(CycleAdjustmentType adjustmentType)
        => adjustmentType switch
        {
            CycleAdjustmentType.LateActivity => "late_activity",
            CycleAdjustmentType.SourceCorrection => "source_correction",
            CycleAdjustmentType.ReconciliationRepair => "reconciliation_repair",
            _ => adjustmentType.ToString()
        };
}
