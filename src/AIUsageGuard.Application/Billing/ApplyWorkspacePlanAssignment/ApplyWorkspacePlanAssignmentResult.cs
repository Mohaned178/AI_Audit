using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Billing.ApplyWorkspacePlanAssignment;

public sealed record ApplyWorkspacePlanAssignmentResult(
    WorkspacePlanAssignment Assignment,
    UsageCycle? OpenedCycle,
    bool ScheduledForFutureCycle);
