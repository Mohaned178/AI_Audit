namespace AIUsageGuard.Api.Contracts.AuditLogs;

public sealed record AuditLogClientContextResponse(
    string? IpAddressHash,
    string? UserAgent);
