using Microsoft.AspNetCore.Mvc;

namespace AIUsageGuard.Api.Security;

public sealed class RequireProtectedRequestIntegrityAttribute : TypeFilterAttribute
{
    public RequireProtectedRequestIntegrityAttribute()
        : base(typeof(ProtectedRequestIntegrityFilter))
    {
    }
}
