using AIUsageGuard.Application.Security;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AIUsageGuard.Api.Security;

public sealed class ProtectedRequestIntegrityFilter : IAsyncActionFilter
{
    private readonly ProtectedRequestIntegrityService _service;

    public ProtectedRequestIntegrityFilter(ProtectedRequestIntegrityService service)
    {
        _service = service;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await _service.EnsureAsync(context.HttpContext.RequestAborted);
        await next();
    }
}
