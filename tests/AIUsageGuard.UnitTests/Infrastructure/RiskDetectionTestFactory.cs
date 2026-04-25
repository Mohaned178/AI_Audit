using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Application.AIUsageEvents.IngestEvent;
using AIUsageGuard.Application.RiskDetection.EvaluateEvent;
using AIUsageGuard.Infrastructure.Auditing;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageGuard.UnitTests.Infrastructure;

internal static class RiskDetectionTestFactory
{
    public static EvaluateAIUsageEventRiskService CreateEvaluationService(IPlatformStore store)
    {
        return new EvaluateAIUsageEventRiskService(
            store,
            new AuditService(store),
            [
                new SensitiveDataPatternRiskEvaluator(),
                new FileUploadRiskEvaluator(),
                new UnapprovedToolRiskEvaluator(),
                new CostThresholdRiskEvaluator()
            ],
            NullLogger<EvaluateAIUsageEventRiskService>.Instance);
    }

    public static IngestAIUsageEventService CreateIngestService(IPlatformStore store)
        => new(
            store,
            new AuditService(store),
            CreateEvaluationService(store),
            BillingTestFactory.CreatePlanAssignmentService(store),
            BillingTestFactory.CreateLimitEvaluator(store));
}
