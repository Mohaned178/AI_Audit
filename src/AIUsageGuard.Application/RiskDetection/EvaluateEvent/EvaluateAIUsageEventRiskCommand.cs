namespace AIUsageGuard.Application.RiskDetection.EvaluateEvent;

public sealed record EvaluateAIUsageEventRiskCommand(
    Guid WorkspaceId,
    Guid EventId,
    Guid RequestedByUserId);
