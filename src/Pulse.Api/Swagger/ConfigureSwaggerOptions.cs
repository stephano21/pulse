using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Pulse.Api.Swagger;

public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                new OpenApiInfo
                {
                    Title = "Pulse API",
                    Version = description.ApiVersion.ToString(),
                    Description = "Sincronización Yapa: productos, clientes, ventas y cobros."
                });
        }

        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Description = "JWT: encabezado Authorization con valor Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
    }
}
