using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.Billing;
using AIUsageGuard.Application.Billing.ApplyWorkspacePlanAssignment;
using AIUsageGuard.Infrastructure.Auditing;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageGuard.UnitTests.Infrastructure;

internal static class BillingTestFactory
{
    public static BillingLimitEvaluator CreateLimitEvaluator(IPlatformStore store)
        => new(store, new AuditService(store), NullLogger<BillingLimitEvaluator>.Instance);

    public static ApplyWorkspacePlanAssignmentService CreatePlanAssignmentService(IPlatformStore store)
        => new(
            store,
            new AuditService(store),
            new BillingDimensionCounter(store),
            CreateLimitEvaluator(store),
            NullLogger<ApplyWorkspacePlanAssignmentService>.Instance);
}
