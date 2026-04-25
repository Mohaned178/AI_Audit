namespace AIUsageGuard.Application.Security;

public sealed class ProtectedRequestIntegrityOptions
{
    public const string SectionName = "Security:ProtectedRequestIntegrity";

    public bool Enabled { get; set; } = true;

    public bool RequireForUnsafeMethods { get; set; } = true;

    public string HeaderName { get; set; } = "X-CSRF-TOKEN";
}
