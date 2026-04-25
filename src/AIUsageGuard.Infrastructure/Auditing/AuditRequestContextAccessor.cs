using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace AIUsageGuard.Infrastructure.Auditing;

public sealed class AuditRequestContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditRequestContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public AuditRequestContext Capture()
        => Capture(_httpContextAccessor.HttpContext);

    public AuditRequestContext Capture(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return new AuditRequestContext(null, null, null);
        }

        var correlationId = string.IsNullOrWhiteSpace(httpContext.TraceIdentifier)
            ? null
            : httpContext.TraceIdentifier;
        var remoteIpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var clientIpAddressHash = string.IsNullOrWhiteSpace(remoteIpAddress)
            ? null
            : HashValue(remoteIpAddress);
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        return new AuditRequestContext(
            correlationId,
            clientIpAddressHash,
            string.IsNullOrWhiteSpace(userAgent) ? null : Truncate(userAgent, 512));
    }

    private static string HashValue(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}

public sealed record AuditRequestContext(
    string? CorrelationId,
    string? ClientIpAddressHash,
    string? UserAgent);
