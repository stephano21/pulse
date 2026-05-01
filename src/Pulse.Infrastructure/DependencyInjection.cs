using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pulse.Application.Abstractions;
using Pulse.Infrastructure.Data;
using Pulse.Infrastructure.Email;
using Pulse.Infrastructure.Identity;
using Pulse.Infrastructure.Sync;

namespace Pulse.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Persistencia + Identity + email + sync. En la API, preferir registrar
    /// <see cref="AddPulsePersistence"/>, JWT, <see cref="AddPulseIdentity"/> y luego el resto (orden tipo SGC).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPulsePersistence(configuration);
        services.AddPulseIdentity();
        services.AddPulseEmailAndSync(configuration);
        return services;
    }

    public static IServiceCollection AddPulsePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Falta ConnectionStrings__Default (o ConnectionStrings:Default). " +
                "Ejemplo: Host=localhost;Port=5432;Database=pulse;Username=pulse;Password=***");
        }

        var looksLikeUri = connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            || connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase);
        if (!connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) && !looksLikeUri)
        {
            throw new InvalidOperationException(
                "Usa una cadena Npgsql con Host=..., o una URI postgres://... / postgresql://... (p. ej. DATABASE_URL en Railway).");
        }

        services.AddDbContext<PulseDbContext>(o => o.UseNpgsql(connectionString));
        return services;
    }

    public static IServiceCollection AddPulseIdentity(this IServiceCollection services)
    {
        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
            })
            .AddEntityFrameworkStores<PulseDbContext>()
            .AddDefaultTokenProviders();
        return services;
    }

    public static IServiceCollection AddPulseEmailAndSync(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        var emailSection = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>();
        if (emailSection?.IsSmtpConfigured == true)
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, LoggingEmailSender>();

        services.AddScoped<ISyncService, SyncService>();
        return services;
    }
}
