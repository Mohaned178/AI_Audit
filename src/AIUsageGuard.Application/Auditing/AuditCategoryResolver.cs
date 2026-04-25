using AIUsageGuard.Application.Models;

namespace AIUsageGuard.Application.Auditing;

public sealed class AuditCategoryResolver
{
    public string ResolveCategory(AuditRecord record)
    {
        var actionType = record.ActionType.Trim();
        var targetType = record.TargetType.Trim();

        if (actionType.StartsWith("auth.", StringComparison.OrdinalIgnoreCase) ||
            actionType.Contains("login", StringComparison.OrdinalIgnoreCase) ||
            targetType.Contains("session", StringComparison.OrdinalIgnoreCase))
        {
            return "authentication";
        }

        if (actionType.StartsWith("authorization.", StringComparison.OrdinalIgnoreCase) ||
            targetType.Contains("authorization", StringComparison.OrdinalIgnoreCase) ||
            targetType.Contains("policy", StringComparison.OrdinalIgnoreCase))
        {
            return "authorization";
        }

        if (actionType.StartsWith("billing.", StringComparison.OrdinalIgnoreCase) ||
            targetType.Contains("billing", StringComparison.OrdinalIgnoreCase))
        {
            return "billing";
        }

        if (actionType.StartsWith("notification.", StringComparison.OrdinalIgnoreCase) ||
            targetType.Contains("notification", StringComparison.OrdinalIgnoreCase))
        {
            return "notification";
        }

        if (actionType.StartsWith("report.", StringComparison.OrdinalIgnoreCase) ||
            targetType.Contains("report", StringComparison.OrdinalIgnoreCase))
        {
            return "reporting";
        }

        return "governance";
    }

    public bool IsSecurityRelevant(AuditRecord record)
    {
        if (string.Equals(record.Result, "denied", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var actionType = record.ActionType.Trim();
        return actionType.StartsWith("auth.", StringComparison.OrdinalIgnoreCase) ||
               actionType.StartsWith("authorization.", StringComparison.OrdinalIgnoreCase) ||
               actionType.Contains("lockout", StringComparison.OrdinalIgnoreCase) ||
               actionType.Contains("integrity", StringComparison.OrdinalIgnoreCase);
    }
}
