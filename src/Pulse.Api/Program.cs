using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pulse.Api.Auth;
using Pulse.Api.Configuration;
using Pulse.Api.Middleware;
using Pulse.Api.Swagger;
using Pulse.Infrastructure;
using Pulse.Infrastructure.Middleware;
using Swashbuckle.AspNetCore.SwaggerGen;

// Railway y otros proveedores suelen exponer solo DATABASE_URL (postgres://...).
var existingConn = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrWhiteSpace(existingConn) && !string.IsNullOrWhiteSpace(databaseUrl))
    Environment.SetEnvironmentVariable("ConnectionStrings__Default", databaseUrl);

EmailEnvironmentBootstrap.Apply();
JwtEnvironmentBootstrap.Apply();

var builder = WebApplication.CreateBuilder(args);

JwtSigningKeyValidator.Validate(builder.Configuration);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection("Authentication:Google"));
builder.Services.AddSingleton<TokenService>();

builder.Services.AddInfrastructure(builder.Configuration);

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddSingleton<IConfigureNamedOptions<JwtBearerOptions>, ConfigurePulseJwtBearerOptions>();

builder.Services.AddAuthorization();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p =>
    {
        if (corsOrigins is { Length: > 0 })
            p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
        else
            p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseMiddleware<RequestIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions.OrderByDescending(d => d.ApiVersion))
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                $"Pulse {description.GroupName.ToUpperInvariant()}");
        }
    });
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();

app.MapControllers();

app.MapGet("/", (IWebHostEnvironment env) =>
    env.IsDevelopment()
        ? Results.Redirect("/swagger/index.html", permanent: false)
        : Results.Redirect("/v1/health", permanent: false))
    .ExcludeFromDescription();

await DatabaseInitializer.MigrateAndSeedAsync(app.Services);

app.Run();
