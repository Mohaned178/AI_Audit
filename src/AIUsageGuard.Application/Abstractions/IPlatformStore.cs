using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Abstractions;

public interface IPlatformStore
{
    Task<UserAccount?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserAccount?> FindUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddUserAsync(UserAccount user, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(UserAccount user, CancellationToken cancellationToken = default);

    Task<Workspace?> FindWorkspaceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Workspace?> FindWorkspaceBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task AddWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default);
    Task UpdateWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken = default);

    Task<WorkspaceMembership?> FindMembershipByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkspaceMembership?> FindActiveMembershipAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkspaceMembership>> ListMembershipsByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkspaceMembership>> ListMembershipsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task AddMembershipAsync(WorkspaceMembership membership, CancellationToken cancellationToken = default);
    Task UpdateMembershipAsync(WorkspaceMembership membership, CancellationToken cancellationToken = default);
    Task<int> CountOwnerMembershipsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<int> CountActiveMembershipsAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    Task<PlanDefinition?> FindPlanDefinitionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PlanDefinition?> FindPlanDefinitionByCodeAsync(string planCode, CancellationToken cancellationToken = default);
    Task<PlanDefinition?> FindDefaultPlanDefinitionAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlanDefinition>> ListActivePlanDefinitionsAsync(CancellationToken cancellationToken = default);
    Task AddPlanDefinitionAsync(PlanDefinition planDefinition, CancellationToken cancellationToken = default);
    Task UpdatePlanDefinitionAsync(PlanDefinition planDefinition, CancellationToken cancellationToken = default);

    Task<PlanLimitRule?> FindPlanLimitRuleAsync(Guid planDefinitionId, BillingDimension dimension, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlanLimitRule>> ListPlanLimitRulesAsync(Guid planDefinitionId, CancellationToken cancellationToken = default);
    Task AddPlanLimitRuleAsync(PlanLimitRule rule, CancellationToken cancellationToken = default);
    Task UpdatePlanLimitRuleAsync(PlanLimitRule rule, CancellationToken cancellationToken = default);

    Task<WorkspacePlanAssignment?> FindPlanAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);
    Task<WorkspacePlanAssignment?> FindActivePlanAssignmentAsync(Guid workspaceId, DateTimeOffset cycleStartUtc, CancellationToken cancellationToken = default);
    Task<WorkspacePlanAssignment?> FindNextPlanAssignmentAsync(Guid workspaceId, DateTimeOffset cycleStartUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkspacePlanAssignment>> ListPlanAssignmentsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task AddPlanAssignmentAsync(WorkspacePlanAssignment assignment, CancellationToken cancellationToken = default);
    Task UpdatePlanAssignmentAsync(WorkspacePlanAssignment assignment, CancellationToken cancellationToken = default);

    Task<UsageCycle?> FindUsageCycleAsync(Guid workspaceId, Guid cycleId, CancellationToken cancellationToken = default);
    Task<UsageCycle?> FindUsageCycleByStartAsync(Guid workspaceId, DateTimeOffset cycleStartUtc, CancellationToken cancellationToken = default);
    Task<UsageCycle?> FindCurrentUsageCycleAsync(Guid workspaceId, DateTimeOffset asOfUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsageCycle>> ListUsageCyclesAsync(Guid workspaceId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsageCycle>> ListUsageCyclesDueForReconciliationAsync(
        DateTimeOffset dueBeforeUtc,
        int maxCount,
        CancellationToken cancellationToken = default);
    Task<int> CountUsageCyclesAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task AddUsageCycleAsync(UsageCycle cycle, CancellationToken cancellationToken = default);
    Task UpdateUsageCycleAsync(UsageCycle cycle, CancellationToken cancellationToken = default);

    Task<UsageCycleMetric?> FindUsageCycleMetricAsync(Guid usageCycleId, BillingDimension dimension, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsageCycleMetric>> ListUsageCycleMetricsAsync(Guid usageCycleId, CancellationToken cancellationToken = default);
    Task AddUsageCycleMetricAsync(UsageCycleMetric metric, CancellationToken cancellationToken = default);
    Task UpdateUsageCycleMetricAsync(UsageCycleMetric metric, CancellationToken cancellationToken = default);

    Task<AuditRecord?> FindAuditRecordAsync(Guid workspaceId, Guid auditLogId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditRecord>> ListAuditRecordsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);
    Task<int> CountAuditRecordsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);
    Task<LimitEvent?> FindLatestLimitEventAsync(Guid workspaceId, Guid usageCycleId, BillingDimension dimension, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LimitEvent>> ListLimitEventsAsync(Guid usageCycleId, CancellationToken cancellationToken = default);
    Task AddLimitEventAsync(LimitEvent limitEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CycleAdjustment>> ListCycleAdjustmentsAsync(Guid usageCycleId, CancellationToken cancellationToken = default);
    Task AddCycleAdjustmentAsync(CycleAdjustment adjustment, CancellationToken cancellationToken = default);

    Task AddAuditAsync(AuditRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditRecord>> ListAuditsAsync(CancellationToken cancellationToken = default);
    Task UpdateUserLockoutAsync(UserAccount user, CancellationToken cancellationToken = default);

    Task<AIUsageEvent?> FindAIUsageEventByIdempotencyKeyAsync(Guid workspaceId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<AIUsageEvent?> FindAIUsageEventByIdAsync(Guid workspaceId, Guid eventId, CancellationToken cancellationToken = default);
    Task AddAIUsageEventAsync(AIUsageEvent aiUsageEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AIUsageEvent>> ListAIUsageEventsAsync(
        Guid workspaceId,
        AIUsageEventType? eventType,
        Guid? actorUserId,
        string? toolName,
        DateTimeOffset? fromOccurredAt,
        DateTimeOffset? toOccurredAt,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> CountAIUsageEventsAsync(
        Guid workspaceId,
        AIUsageEventType? eventType,
        Guid? actorUserId,
        string? toolName,
        DateTimeOffset? fromOccurredAt,
        DateTimeOffset? toOccurredAt,
        CancellationToken cancellationToken = default);
    Task<int> CountAcceptedAIUsageEventsAsync(
        Guid workspaceId,
        DateTimeOffset fromOccurredAt,
        DateTimeOffset toOccurredAtExclusive,
        CancellationToken cancellationToken = default);
    Task<decimal> SumAIUsageEventCostsAsync(
        Guid workspaceId,
        DateTimeOffset? fromOccurredAt,
        DateTimeOffset? toOccurredAt,
        CancellationToken cancellationToken = default);
    Task<DashboardTotals> GetDashboardTotalsAsync(
        Guid workspaceId,
        ReportingPeriod period,
        CancellationToken cancellationToken = default);
    Task<UsageSummaryPage> GetUsageSummaryByUserAsync(
        Guid workspaceId,
        ReportingPeriod period,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<UsageSummaryPage> GetUsageSummaryByToolAsync(
        Guid workspaceId,
        ReportingPeriod period,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<AlertsSummary> GetAlertsSummaryAsync(
        Guid workspaceId,
        ReportingPeriod period,
        CancellationToken cancellationToken = default);
    Task<EstimatedCostSummary> GetEstimatedCostSummaryAsync(
        Guid workspaceId,
        ReportingPeriod period,
        CancellationToken cancellationToken = default);

    Task<RiskFinding?> FindRiskFindingAsync(Guid workspaceId, Guid findingId, CancellationToken cancellationToken = default);
    Task<RiskFinding?> FindRiskFindingByEventAndRuleAsync(Guid workspaceId, Guid eventId, RiskRuleType ruleType, CancellationToken cancellationToken = default);
    Task AddRiskFindingAsync(RiskFinding riskFinding, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RiskFinding>> ListRiskFindingsAsync(
        Guid workspaceId,
        RiskRuleType? ruleType,
        RiskSeverity? severity,
        RiskFindingStatus? status,
        Guid? actorUserId,
        string? toolName,
        DateTimeOffset? fromDetectedAt,
        DateTimeOffset? toDetectedAt,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> CountRiskFindingsAsync(
        Guid workspaceId,
        RiskRuleType? ruleType,
        RiskSeverity? severity,
        RiskFindingStatus? status,
        Guid? actorUserId,
        string? toolName,
        DateTimeOffset? fromDetectedAt,
        DateTimeOffset? toDetectedAt,
        CancellationToken cancellationToken = default);

    Task<RiskEvaluationOutcome?> FindRiskEvaluationOutcomeByEventIdAsync(Guid workspaceId, Guid eventId, CancellationToken cancellationToken = default);
    Task AddRiskEvaluationOutcomeAsync(RiskEvaluationOutcome outcome, CancellationToken cancellationToken = default);

    Task<WorkspaceRiskPolicy?> FindWorkspaceRiskPolicyAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task AddWorkspaceRiskPolicyAsync(WorkspaceRiskPolicy policy, CancellationToken cancellationToken = default);
    Task UpdateWorkspaceRiskPolicyAsync(WorkspaceRiskPolicy policy, CancellationToken cancellationToken = default);

    Task<NotificationPreference?> FindNotificationPreferenceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationPreference>> ListNotificationPreferencesAsync(
        bool? urgentAlertsEnabled = null,
        bool? digestEnabled = null,
        CancellationToken cancellationToken = default);
    Task AddNotificationPreferenceAsync(NotificationPreference preference, CancellationToken cancellationToken = default);
    Task UpdateNotificationPreferenceAsync(NotificationPreference preference, CancellationToken cancellationToken = default);

    Task AddBackgroundJobRunAsync(BackgroundJobRun run, CancellationToken cancellationToken = default);
    Task UpdateBackgroundJobRunAsync(BackgroundJobRun run, CancellationToken cancellationToken = default);

    Task<NotificationMessage?> FindNotificationAsync(Guid workspaceId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<NotificationMessage?> FindNotificationByFingerprintAsync(
        Guid workspaceId,
        NotificationType notificationType,
        string triggerFingerprint,
        CancellationToken cancellationToken = default);
    Task AddNotificationAsync(NotificationMessage notification, CancellationToken cancellationToken = default);
    Task UpdateNotificationAsync(NotificationMessage notification, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationMessage>> ListNotificationsAsync(
        Guid workspaceId,
        NotificationType? notificationType,
        NotificationStatus? status,
        DateTimeOffset? fromCreatedAt,
        DateTimeOffset? toCreatedAt,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> CountNotificationsAsync(
        Guid workspaceId,
        NotificationType? notificationType,
        NotificationStatus? status,
        DateTimeOffset? fromCreatedAt,
        DateTimeOffset? toCreatedAt,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationMessage>> ListNotificationsPendingDeliveryAsync(
        DateTimeOffset asOfUtc,
        int maxCount,
        CancellationToken cancellationToken = default);

    Task AddNotificationDeliveryOutcomeAsync(NotificationDeliveryOutcome outcome, CancellationToken cancellationToken = default);
    Task UpdateNotificationDeliveryOutcomeAsync(NotificationDeliveryOutcome outcome, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDeliveryOutcome>> ListNotificationDeliveryOutcomesAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDeliveryOutcome>> ListDueNotificationRetryOutcomesAsync(
        DateTimeOffset asOfUtc,
        int maxCount,
        CancellationToken cancellationToken = default);
}
