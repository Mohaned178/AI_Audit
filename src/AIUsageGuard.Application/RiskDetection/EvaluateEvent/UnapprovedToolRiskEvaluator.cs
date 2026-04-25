using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.EvaluateEvent;

public sealed class UnapprovedToolRiskEvaluator : IRiskRuleEvaluator
{
    public RiskRuleType RuleType => RiskRuleType.UnapprovedTool;

    public Task<IReadOnlyList<RiskRuleMatch>> EvaluateAsync(
        AIUsageEvent eventRecord,
        WorkspaceRiskPolicy policy,
        decimal dailyEstimatedCost,
        CancellationToken cancellationToken = default)
    {
        _ = dailyEstimatedCost;
        _ = cancellationToken;

        if (policy.ApprovedTools.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<RiskRuleMatch>>([]);
        }

        var isApproved = policy.ApprovedTools.Any(tool => string.Equals(tool, eventRecord.ToolName, StringComparison.OrdinalIgnoreCase));
        if (isApproved)
        {
            return Task.FromResult<IReadOnlyList<RiskRuleMatch>>([]);
        }

        return Task.FromResult<IReadOnlyList<RiskRuleMatch>>(
            [new RiskRuleMatch(
                RiskRuleType.UnapprovedTool,
                RiskSeverity.High,
                $"Tool '{eventRecord.ToolName}' is not approved for this workspace.",
                eventRecord.ToolName)]);
    }
}
