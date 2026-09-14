using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pulse.Domain;
using Pulse.Infrastructure.Data;
using Pulse.Infrastructure.Identity;

namespace Pulse.Infrastructure;

public static class DatabaseInitializer
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task MigrateAndSeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        if (!await db.Tenants.AnyAsync(cancellationToken))
        {
            db.Tenants.Add(new Tenant { Id = DefaultTenantId, Name = "default", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync(cancellationToken);
        }

        await SeedSuperAdminsAsync(scope.ServiceProvider, cancellationToken);
    }

    /// <summary>
    /// Crea el rol SuperAdmin (si falta), crea la cuenta indicada en App:SuperAdminSeed si aún no existe,
    /// y promueve los correos listados en App:SuperAdminEmails que ya existan como usuarios.
    /// Idempotente: se re-ejecuta en cada arranque, nunca pisa una contraseña existente.
    /// </summary>
    private static async Task SeedSuperAdminsAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        if (!await roleManager.RoleExistsAsync(Roles.SuperAdmin))
            await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.SuperAdmin));

        var seedEmail = configuration["App:SuperAdminSeed:Email"];
        var seedPassword = configuration["App:SuperAdminSeed:Password"];
        if (!string.IsNullOrWhiteSpace(seedEmail) && !string.IsNullOrWhiteSpace(seedPassword))
        {
            var existing = await userManager.FindByEmailAsync(seedEmail);
            if (existing is null)
            {
                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = seedEmail,
                    Email = seedEmail,
                    EmailConfirmed = true,
                    TenantId = DefaultTenantId,
                    AuthProvider = AuthProviders.Local,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                var created = await userManager.CreateAsync(user, seedPassword);
                if (created.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
                    logger.LogInformation("Usuario SuperAdmin sembrado desde App:SuperAdminSeed: {Email}.", seedEmail);
                }
                else
                {
                    logger.LogWarning(
                        "No se pudo sembrar el usuario SuperAdmin {Email}: {Errors}.",
                        seedEmail,
                        string.Join(" ", created.Errors.Select(e => e.Description)));
                }
            }
            else if (!await userManager.IsInRoleAsync(existing, Roles.SuperAdmin))
            {
                await userManager.AddToRoleAsync(existing, Roles.SuperAdmin);
            }
        }

        var emails = configuration.GetSection("App:SuperAdminEmails").Get<string[]>() ?? [];
        foreach (var email in emails.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(email))
                continue;

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                logger.LogWarning(
                    "App:SuperAdminEmails incluye {Email} pero aún no existe como usuario; se omite hasta el próximo arranque tras el registro.",
                    email);
                continue;
            }

            if (!await userManager.IsInRoleAsync(user, Roles.SuperAdmin))
                await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
        }
    }
}
