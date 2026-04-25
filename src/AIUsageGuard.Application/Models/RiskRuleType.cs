namespace AIUsageGuard.Application.Models;

public enum RiskRuleType
{
    SensitiveDataPattern = 0,
    FileUpload = 1,
    UnapprovedTool = 2,
    CostThresholdExceeded = 3
}
