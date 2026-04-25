using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.EvaluateEvent;

public sealed class FileUploadRiskEvaluator : IRiskRuleEvaluator
{
    public RiskRuleType RuleType => RiskRuleType.FileUpload;

    public Task<IReadOnlyList<RiskRuleMatch>> EvaluateAsync(
        AIUsageEvent eventRecord,
        WorkspaceRiskPolicy policy,
        decimal dailyEstimatedCost,
        CancellationToken cancellationToken = default)
    {
        _ = policy;
        _ = dailyEstimatedCost;
        _ = cancellationToken;

        if (eventRecord.EventType == AIUsageEventType.FileUploaded || !string.IsNullOrWhiteSpace(eventRecord.FileName))
        {
            var reason = string.IsNullOrWhiteSpace(eventRecord.FileName)
                ? "Event includes file upload metadata."
                : $"Event uploaded file '{eventRecord.FileName}'.";

            return Task.FromResult<IReadOnlyList<RiskRuleMatch>>(
                [new RiskRuleMatch(RiskRuleType.FileUpload, RiskSeverity.Medium, reason, eventRecord.FileName)]);
        }

        return Task.FromResult<IReadOnlyList<RiskRuleMatch>>([]);
    }
}
