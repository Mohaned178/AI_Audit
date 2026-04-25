using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Models;

namespace AIUsageGuard.UnitTests.Auditing;

public sealed class AuditCategoryResolverTests
{
    [Theory]
    [InlineData("auth.login", "session", "authentication", true)]
    [InlineData("authorization.denied", "audit_log", "authorization", true)]
    [InlineData("billing.plan_status.read", "plan_status", "billing", false)]
    [InlineData("workspace.context.read", "workspace", "governance", false)]
    public void ResolveCategory_and_security_relevance_follow_action_shape(
        string actionType,
        string targetType,
        string expectedCategory,
        bool expectedSecurityRelevant)
    {
        var resolver = new AuditCategoryResolver();
        var record = new AuditRecord
        {
            ActionType = actionType,
            TargetType = targetType,
            Result = expectedSecurityRelevant ? "denied" : "success"
        };

        Assert.Equal(expectedCategory, resolver.ResolveCategory(record));
        Assert.Equal(expectedSecurityRelevant, resolver.IsSecurityRelevant(record));
    }
}
