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
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Default para SQL Server.");
        services.AddDbContext<PulseDbContext>(o => o.UseSqlServer(connectionString));

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

        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddScoped<ISyncService, SyncService>();
        return services;
    }
}
