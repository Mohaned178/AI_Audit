using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.RiskDetection.GetRiskPolicy;

public sealed record GetWorkspaceRiskPolicyResult(
    WorkspaceRiskPolicy Policy);
