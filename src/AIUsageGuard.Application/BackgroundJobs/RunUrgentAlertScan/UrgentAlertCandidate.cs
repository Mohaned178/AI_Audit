using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.BackgroundJobs.RunUrgentAlertScan;

public sealed record UrgentAlertCandidate(
    Guid WorkspaceId,
    Guid FindingId,
    string TriggerFingerprint,
    UrgentAlertSnapshot Snapshot);
