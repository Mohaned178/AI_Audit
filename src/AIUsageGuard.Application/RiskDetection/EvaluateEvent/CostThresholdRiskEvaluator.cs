using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.EvaluateEvent;

public sealed class CostThresholdRiskEvaluator : IRiskRuleEvaluator
{
    public RiskRuleType RuleType => RiskRuleType.CostThresholdExceeded;

    public Task<IReadOnlyList<RiskRuleMatch>> EvaluateAsync(
        AIUsageEvent eventRecord,
        WorkspaceRiskPolicy policy,
        decimal dailyEstimatedCost,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        var matches = new List<RiskRuleMatch>();

        if (policy.PerEventEstimatedCostThreshold is decimal perEventThreshold &&
            eventRecord.EstimatedCost is decimal currentCost &&
            currentCost > perEventThreshold)
        {
            matches.Add(new RiskRuleMatch(
                RiskRuleType.CostThresholdExceeded,
                RiskSeverity.Medium,
                $"Estimated cost {currentCost:0.####} exceeds the per-event threshold of {perEventThreshold:0.####}.",
                currentCost.ToString("0.####")));
        }

        if (policy.DailyEstimatedCostThreshold is decimal dailyThreshold && dailyEstimatedCost > dailyThreshold)
        {
            matches.Add(new RiskRuleMatch(
                RiskRuleType.CostThresholdExceeded,
                RiskSeverity.Medium,
                $"Workspace estimated cost {dailyEstimatedCost:0.####} exceeds the daily threshold of {dailyThreshold:0.####}.",
                dailyEstimatedCost.ToString("0.####")));
        }

        return Task.FromResult<IReadOnlyList<RiskRuleMatch>>(matches.Count == 0 ? [] : [matches[0]]);
    }
}
