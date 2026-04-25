using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.EvaluateEvent;

public sealed record RiskRuleMatch(
    RiskRuleType RuleType,
    RiskSeverity Severity,
    string Reason,
    string? EvidencePreview);
