using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.EvaluateEvent;

public interface IRiskRuleEvaluator
{
    RiskRuleType RuleType { get; }

    Task<IReadOnlyList<RiskRuleMatch>> EvaluateAsync(
        AIUsageEvent eventRecord,
        WorkspaceRiskPolicy policy,
        decimal dailyEstimatedCost,
        CancellationToken cancellationToken = default);
}
