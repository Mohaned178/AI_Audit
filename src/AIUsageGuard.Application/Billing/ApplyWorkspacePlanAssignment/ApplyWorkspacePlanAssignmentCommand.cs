namespace AIUsageGuard.Application.Billing.ApplyWorkspacePlanAssignment;

public sealed record ApplyWorkspacePlanAssignmentCommand(
    Guid WorkspaceId,
    Guid RequestedByUserId,
    string PlanCode,
    DateTimeOffset EffectiveFromCycleStartUtc,
    string? ChangeReason);
