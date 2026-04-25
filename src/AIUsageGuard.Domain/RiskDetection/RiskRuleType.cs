namespace AIUsageGuard.Domain.RiskDetection;

public enum RiskRuleType
{
    SensitiveDataPattern = 0,
    FileUpload = 1,
    UnapprovedTool = 2,
    CostThresholdExceeded = 3
}
