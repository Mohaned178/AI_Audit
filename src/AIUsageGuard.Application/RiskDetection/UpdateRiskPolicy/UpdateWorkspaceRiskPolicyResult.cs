using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.UpdateRiskPolicy;

public sealed record UpdateWorkspaceRiskPolicyResult(
    WorkspaceRiskPolicy Policy,
    bool CreatedNewPolicy);
