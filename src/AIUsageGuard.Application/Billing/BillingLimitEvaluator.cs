using System.Diagnostics.Metrics;
using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Models;
using Microsoft.Extensions.Logging;

namespace AIUsageGuard.Application.Billing;

public sealed class BillingLimitEvaluator
{
    private static readonly Meter Meter = new("AIUsageGuard.Billing");
    private static readonly Counter<long> WarningCounter = Meter.CreateCounter<long>("ai_usage_guard.billing.warning");
    private static readonly Counter<long> OverageCounter = Meter.CreateCounter<long>("ai_usage_guard.billing.overage");
    private static readonly Counter<long> RestrictionCounter = Meter.CreateCounter<long>("ai_usage_guard.billing.restriction");

    private readonly IPlatformStore _store;
    private readonly IAuditService _auditService;
    private readonly ILogger<BillingLimitEvaluator> _logger;

    public BillingLimitEvaluator(
        IPlatformStore store,
        IAuditService auditService,
        ILogger<BillingLimitEvaluator> logger)
    {
        _store = store;
        _auditService = auditService;
        _logger = logger;
    }

    public bool WouldExceedHardLimit(UsageCycleMetric metric, decimal projectedQuantity)
        => metric.LimitBehavior == BillingLimitBehavior.Restrict &&
           metric.HardLimitQuantity.HasValue &&
           projectedQuantity > metric.HardLimitQuantity.Value;

    public BillingLimitEvaluationResult Evaluate(
        UsageCycleMetric metric,
        decimal newQuantity,
        DateTimeOffset observedAtUtc)
    {
        var priorState = metric.State;
        var nextState = ResolveState(
            metric.LimitBehavior,
            metric.WarningThresholdQuantity,
            metric.HardLimitQuantity,
            metric.IncludedQuantity,
            newQuantity);
        var overageQuantity = metric.LimitBehavior == BillingLimitBehavior.AllowOverage
            ? Math.Max(0m, newQuantity - metric.IncludedQuantity)
            : 0m;

        metric.CurrentQuantity = newQuantity;
        metric.OverageQuantity = overageQuantity;
        metric.State = nextState;
        if (nextState != priorState)
        {
            metric.LastTransitionAtUtc = observedAtUtc;
        }

        return new BillingLimitEvaluationResult(
            metric,
            priorState,
            nextState,
            nextState != priorState,
            ResolveEventType(priorState, nextState),
            ResolveThreshold(metric, nextState));
    }

    public async Task<BillingLimitEvaluationResult> ApplyAsync(
        Guid workspaceId,
        UsageCycleMetric metric,
        decimal newQuantity,
        DateTimeOffset observedAtUtc,
        string sourceType,
        string sourceId,
        CancellationToken cancellationToken = default)
    {
        var result = Evaluate(metric, newQuantity, observedAtUtc);
        await _store.UpdateUsageCycleMetricAsync(metric, cancellationToken);

        if (!result.StateChanged || result.TransitionEventType is null)
        {
            return result;
        }

        var latestEvent = await _store.FindLatestLimitEventAsync(workspaceId, metric.UsageCycleId, metric.Dimension, cancellationToken);
        if (latestEvent is not null &&
            latestEvent.EventType == result.TransitionEventType.Value &&
            latestEvent.CurrentQuantity == newQuantity &&
            latestEvent.ThresholdQuantity == result.ThresholdQuantity)
        {
            return result;
        }

        var reason = BuildReason(metric.Dimension, result.CurrentState, newQuantity, result.ThresholdQuantity);
        var limitEvent = new LimitEvent
        {
            WorkspaceId = workspaceId,
            UsageCycleId = metric.UsageCycleId,
            Dimension = metric.Dimension,
            EventType = result.TransitionEventType.Value,
            TriggeredBySourceType = sourceType,
            TriggeredBySourceId = sourceId,
            CurrentQuantity = newQuantity,
            ThresholdQuantity = result.ThresholdQuantity,
            Reason = reason,
            OccurredAtUtc = observedAtUtc
        };

        await _store.AddLimitEventAsync(limitEvent, cancellationToken);
        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = workspaceId,
            ActionType = ToAuditAction(result.TransitionEventType.Value),
            TargetType = "limit_event",
            TargetId = limitEvent.Id.ToString(),
            Result = "success",
            Reason = reason
        }, cancellationToken);

        IncrementCounter(result.TransitionEventType.Value, workspaceId, metric.Dimension);
        _logger.LogInformation(
            "Recorded billing limit transition {EventType} for workspace {WorkspaceId}, cycle {UsageCycleId}, dimension {Dimension}, quantity {Quantity}.",
            result.TransitionEventType.Value,
            workspaceId,
            metric.UsageCycleId,
            metric.Dimension,
            newQuantity);

        return result;
    }

    public async Task RecordDeniedActionAsync(
        Guid workspaceId,
        Guid? actorUserId,
        BillingDimension dimension,
        decimal attemptedQuantity,
        decimal thresholdQuantity,
        string actionType,
        CancellationToken cancellationToken = default)
    {
        var reason = $"{ToLabel(dimension)} would exceed the restricted plan limit of {thresholdQuantity:0.##}. Attempted quantity {attemptedQuantity:0.##} was denied.";

        await _auditService.RecordAsync(new AuditRecord
        {
            WorkspaceId = workspaceId,
            ActorUserId = actorUserId,
            ActionType = actionType,
            TargetType = "billing_limit",
            Result = "denied",
            Reason = reason
        }, cancellationToken);
    }

    private static UsageCycleMetricState ResolveState(
        BillingLimitBehavior behavior,
        decimal? warningThresholdQuantity,
        decimal? hardLimitQuantity,
        decimal includedQuantity,
        decimal currentQuantity)
    {
        if (behavior == BillingLimitBehavior.Restrict && hardLimitQuantity.HasValue && currentQuantity >= hardLimitQuantity.Value)
        {
            return UsageCycleMetricState.Restricted;
        }

        if (behavior == BillingLimitBehavior.AllowOverage && currentQuantity > includedQuantity)
        {
            return UsageCycleMetricState.Overage;
        }

        if (warningThresholdQuantity.HasValue && currentQuantity >= warningThresholdQuantity.Value)
        {
            return UsageCycleMetricState.Warning;
        }

        return UsageCycleMetricState.WithinLimit;
    }

    private static LimitEventType? ResolveEventType(UsageCycleMetricState previous, UsageCycleMetricState current)
    {
        if (current == previous)
        {
            return null;
        }

        return current switch
        {
            UsageCycleMetricState.Warning => LimitEventType.WarningRaised,
            UsageCycleMetricState.Overage => LimitEventType.OverageStarted,
            UsageCycleMetricState.Restricted => LimitEventType.RestrictionApplied,
            UsageCycleMetricState.WithinLimit => LimitEventType.StateReturnedToWithinLimit,
            _ => null
        };
    }

    private static decimal? ResolveThreshold(UsageCycleMetric metric, UsageCycleMetricState state)
    {
        return state switch
        {
            UsageCycleMetricState.Warning => metric.WarningThresholdQuantity,
            UsageCycleMetricState.Overage => metric.IncludedQuantity,
            UsageCycleMetricState.Restricted => metric.HardLimitQuantity,
            _ => null
        };
    }

    private static string BuildReason(
        BillingDimension dimension,
        UsageCycleMetricState state,
        decimal currentQuantity,
        decimal? thresholdQuantity)
    {
        var label = ToLabel(dimension);
        return state switch
        {
            UsageCycleMetricState.Warning => $"{label} crossed the warning threshold at {thresholdQuantity:0.##} with current quantity {currentQuantity:0.##}.",
            UsageCycleMetricState.Overage => $"{label} moved into overage after exceeding the included allowance of {thresholdQuantity:0.##}.",
            UsageCycleMetricState.Restricted => $"{label} reached the restricted plan limit of {thresholdQuantity:0.##}.",
            UsageCycleMetricState.WithinLimit => $"{label} returned to within the included allowance.",
            _ => $"{label} state changed."
        };
    }

    private static string ToAuditAction(LimitEventType eventType)
        => eventType switch
        {
            LimitEventType.WarningRaised => "billing.warning",
            LimitEventType.OverageStarted => "billing.overage",
            LimitEventType.RestrictionApplied => "billing.restriction",
            LimitEventType.StateReturnedToWithinLimit => "billing.limit_reset",
            _ => "billing.limit_event"
        };

    private static string ToLabel(BillingDimension dimension)
        => dimension switch
        {
            BillingDimension.ActiveMembers => "Active member usage",
            BillingDimension.AIActivityEvents => "AI activity usage",
            BillingDimension.EstimatedCost => "Estimated cost usage",
            _ => dimension.ToString()
        };

    private static void IncrementCounter(LimitEventType eventType, Guid workspaceId, BillingDimension dimension)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("workspace.id", workspaceId),
            new KeyValuePair<string, object?>("billing.dimension", dimension.ToString())
        };

        switch (eventType)
        {
            case LimitEventType.WarningRaised:
                WarningCounter.Add(1, tags);
                break;
            case LimitEventType.OverageStarted:
                OverageCounter.Add(1, tags);
                break;
            case LimitEventType.RestrictionApplied:
                RestrictionCounter.Add(1, tags);
                break;
        }
    }
}

public sealed record BillingLimitEvaluationResult(
    UsageCycleMetric Metric,
    UsageCycleMetricState PreviousState,
    UsageCycleMetricState CurrentState,
    bool StateChanged,
    LimitEventType? TransitionEventType,
    decimal? ThresholdQuantity);
