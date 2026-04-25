namespace AIUsageGuard.Application.Billing.GetPlanStatus;

public sealed record GetPlanStatusQuery(
    Guid WorkspaceId,
    Guid RequestedByUserId);
