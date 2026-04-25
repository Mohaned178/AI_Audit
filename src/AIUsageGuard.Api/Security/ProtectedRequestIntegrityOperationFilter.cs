using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AIUsageGuard.Api.Security;

public sealed class ProtectedRequestIntegrityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!RequiresProtectedRequestIntegrity(context))
        {
            return;
        }

        operation.Parameters ??= [];

        if (operation.Parameters.Any(parameter => string.Equals(parameter.Name, "X-CSRF-TOKEN", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-CSRF-TOKEN",
            In = ParameterLocation.Header,
            Required = true,
            Description = "Anti-forgery token required for protected write endpoints.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });
    }

    private static bool RequiresProtectedRequestIntegrity(OperationFilterContext context)
    {
        return context.MethodInfo.GetCustomAttributes<RequireProtectedRequestIntegrityAttribute>(inherit: true).Any()
            || context.MethodInfo.DeclaringType?
                .GetCustomAttributes<RequireProtectedRequestIntegrityAttribute>(inherit: true)
                .Any() == true;
    }
}
