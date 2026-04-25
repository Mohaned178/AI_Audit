using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.GetFinding;

public sealed record GetRiskFindingResult(
    RiskFinding Finding,
    AIUsageEvent EventRecord,
    RiskEvaluationOutcome Outcome);
