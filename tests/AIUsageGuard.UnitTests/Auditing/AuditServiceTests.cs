using System.Net;
using AIUsageGuard.Application.Auditing;
using AIUsageGuard.Application.Models;
using AIUsageGuard.Infrastructure.Auditing;
using AIUsageGuard.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.UnitTests.Auditing;

public sealed class AuditServiceTests
{
    [Fact]
    public async Task Record_enriches_category_security_relevance_and_request_context()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "corr-123";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        httpContext.Request.Headers.UserAgent = "Mozilla/5.0";

        var accessor = new AuditRequestContextAccessor(new HttpContextAccessor { HttpContext = httpContext });
        var service = new AuditService(dbContext, new AuditCategoryResolver(), accessor);

        await service.RecordAsync(new AuditRecord
        {
            WorkspaceId = Guid.NewGuid(),
            ActorUserId = Guid.NewGuid(),
            ActionType = "authorization.denied",
            TargetType = "audit_log_history",
            Result = "denied",
            Reason = "Denied."
        });

        var persisted = await dbContext.AuditRecords.SingleAsync();

        Assert.Equal("authorization", persisted.Category);
        Assert.True(persisted.IsSecurityRelevant);
        Assert.Equal("corr-123", persisted.CorrelationId);
        Assert.NotNull(persisted.ClientIpAddressHash);
        Assert.Equal(64, persisted.ClientIpAddressHash!.Length);
        Assert.Equal("Mozilla/5.0", persisted.UserAgent);
    }
}
