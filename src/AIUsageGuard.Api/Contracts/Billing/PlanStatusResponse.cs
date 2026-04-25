namespace AIUsageGuard.Api.Contracts.Billing;

public sealed record PlanStatusResponse(
    Guid WorkspaceId,
    PlanAssignmentSummaryResponse Plan,
    CurrentCycleSummaryResponse CurrentCycle,
    IReadOnlyList<PlanMetricStatusResponse> Metrics);
