using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.EvaluateEvent;

public sealed record EvaluateAIUsageEventRiskResult(
    RiskEvaluationOutcome Outcome,
    IReadOnlyList<RiskFinding> Findings,
    bool CreatedNewOutcome);
