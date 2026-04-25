namespace AIUsageGuard.Api.Contracts.RiskDetection;

public sealed record ListRiskFindingsRequest(
    string? RuleType,
    string? Severity,
    string? Status,
    Guid? ActorUserId,
    string? ToolName,
    DateTimeOffset? FromDetectedAt,
    DateTimeOffset? ToDetectedAt,
    int PageNumber = 1,
    int PageSize = 50);
