using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.ListFindings;

public sealed record ListRiskFindingsQuery(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    RiskRuleType? RuleType,
    RiskSeverity? Severity,
    RiskFindingStatus? Status,
    Guid? ActorUserId,
    string? ToolName,
    DateTimeOffset? FromDetectedAt,
    DateTimeOffset? ToDetectedAt,
    int PageNumber,
    int PageSize);
