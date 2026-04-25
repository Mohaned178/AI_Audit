namespace AIUsageGuard.Api.Contracts.Billing;

public sealed record PlanAssignmentSummaryResponse(
    string PlanCode,
    string DisplayName,
    DateTimeOffset AssignedFromCycleStartUtc,
    string? NextPlanCode,
    DateTimeOffset? NextPlanStartsAtUtc);
