using AIUsageGuard.Application.Billing;
using AIUsageGuard.Application.Models;
using AIUsageGuard.UnitTests.Infrastructure;

namespace AIUsageGuard.UnitTests.Billing;

public sealed class BillingLimitEvaluatorTests
{
    [Fact]
    public async Task Evaluate_moves_allow_overage_metric_into_overage()
    {
        await using var store = TestDbContextFactory.CreateContext();
        var evaluator = BillingTestFactory.CreateLimitEvaluator(store);
        var metric = new UsageCycleMetric
        {
            UsageCycleId = Guid.NewGuid(),
            Dimension = BillingDimension.AIActivityEvents,
            IncludedQuantity = 100m,
            WarningThresholdQuantity = 90m,
            LimitBehavior = BillingLimitBehavior.AllowOverage
        };

        var result = evaluator.Evaluate(metric, 125m, DateTimeOffset.UtcNow);

        Assert.Equal(UsageCycleMetricState.Overage, result.CurrentState);
        Assert.Equal(25m, metric.OverageQuantity);
        Assert.Equal(LimitEventType.OverageStarted, result.TransitionEventType);
    }

    [Fact]
    public async Task Evaluate_marks_restrict_metric_as_restricted_at_hard_limit()
    {
        await using var store = TestDbContextFactory.CreateContext();
        var evaluator = BillingTestFactory.CreateLimitEvaluator(store);
        var metric = new UsageCycleMetric
        {
            UsageCycleId = Guid.NewGuid(),
            Dimension = BillingDimension.ActiveMembers,
            IncludedQuantity = 10m,
            WarningThresholdQuantity = 8m,
            HardLimitQuantity = 10m,
            LimitBehavior = BillingLimitBehavior.Restrict,
            State = UsageCycleMetricState.Warning
        };

        var result = evaluator.Evaluate(metric, 10m, DateTimeOffset.UtcNow);

        Assert.Equal(UsageCycleMetricState.Restricted, result.CurrentState);
        Assert.Equal(LimitEventType.RestrictionApplied, result.TransitionEventType);
    }

    [Fact]
    public async Task WouldExceedHardLimit_allows_quantity_at_limit_and_rejects_above_limit()
    {
        await using var store = TestDbContextFactory.CreateContext();
        var evaluator = BillingTestFactory.CreateLimitEvaluator(store);
        var metric = new UsageCycleMetric
        {
            UsageCycleId = Guid.NewGuid(),
            Dimension = BillingDimension.ActiveMembers,
            IncludedQuantity = 10m,
            WarningThresholdQuantity = 8m,
            HardLimitQuantity = 10m,
            LimitBehavior = BillingLimitBehavior.Restrict
        };

        Assert.False(evaluator.WouldExceedHardLimit(metric, 10m));
        Assert.True(evaluator.WouldExceedHardLimit(metric, 11m));
    }
}
