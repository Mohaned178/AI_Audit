using System.Security.Claims;
using AIUsageGuard.Application.Security;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AIUsageGuard.UnitTests.Security;

public sealed class ProtectedRequestIntegrityServiceTests
{
    [Fact]
    public async Task Ensure_rejects_missing_token_and_records_audit()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Put;
        httpContext.Request.Path = $"/workspaces/{workspaceId}/risk-policy";
        httpContext.Request.RouteValues["workspaceId"] = workspaceId.ToString();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));

        var service = new ProtectedRequestIntegrityService(
            new AuditService(dbContext),
            new HttpContextAccessor { HttpContext = httpContext },
            Options.Create(new ProtectedRequestIntegrityOptions()));

        var exception = await Assert.ThrowsAsync<AIUsageGuard.Application.Errors.RequestFailureException>(() => service.EnsureAsync());

        Assert.Equal(400, exception.StatusCode);
        Assert.True(await dbContext.AuditRecords.AnyAsync(item =>
            item.ActionType == "risk_policy.update" &&
            item.TargetType == "risk_policy" &&
            item.Result == "denied"));
    }

    [Fact]
    public async Task Ensure_allows_requests_with_token()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        var workspaceId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Put;
        httpContext.Request.Path = $"/workspaces/{workspaceId}/notification-preferences";
        httpContext.Request.RouteValues["workspaceId"] = workspaceId.ToString();
        httpContext.Request.Headers["X-CSRF-TOKEN"] = "token";

        var service = new ProtectedRequestIntegrityService(
            new AuditService(dbContext),
            new HttpContextAccessor { HttpContext = httpContext },
            Options.Create(new ProtectedRequestIntegrityOptions()));

        await service.EnsureAsync();

        Assert.Empty(dbContext.AuditRecords);
    }
}
