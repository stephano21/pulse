using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Pulse.Api.Swagger;

public sealed class BearerSecurityRequirementFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference("Bearer"), [] }
        });
    }
}
