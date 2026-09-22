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
        await SeedTenantRolesAsync(scope.ServiceProvider, cancellationToken);
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

    /// <summary>
    /// Crea los roles Dueno/Gerente/Vendedor (si faltan) y rellena tenants de antes de que existiera
    /// este modelo: el usuario más antiguo de cada tenant sin ningún rol propio pasa a ser Dueño (es
    /// quien más probablemente registró el negocio), y el resto de sus compañeros sin rol quedan
    /// como Vendedor (así es como ya venían usando la app). Idempotente: se re-ejecuta en cada
    /// arranque, no toca usuarios que ya tengan alguno de estos tres roles.
    /// </summary>
    private static async Task SeedTenantRolesAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<PulseDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        foreach (var role in new[] { Roles.Dueno, Roles.Gerente, Roles.Vendedor })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        var tenantRoleIds = await db.Roles
            .Where(r => r.Name == Roles.Dueno || r.Name == Roles.Gerente || r.Name == Roles.Vendedor)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var usersWithTenantRole = await db.UserRoles
            .Where(ur => tenantRoleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var usersWithTenantRoleSet = usersWithTenantRole.ToHashSet();

        var usersByTenant = await db.Users
            .Where(u => u.TenantId != null)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new { u.Id, u.TenantId, u.CreatedAt })
            .ToListAsync(cancellationToken);

        foreach (var group in usersByTenant.GroupBy(u => u.TenantId!.Value))
        {
            var membersSinRol = group.Where(u => !usersWithTenantRoleSet.Contains(u.Id)).ToList();
            if (membersSinRol.Count == 0)
                continue;

            var yaTieneDueno = group.Any(u => usersWithTenantRoleSet.Contains(u.Id));
            // Si el tenant ya tenía a alguien con rol propio, no asumimos quién debería ser el
            // Dueño entre los que faltan — todos quedan como Vendedor. Si nadie tenía rol
            // (tenant íntegramente pre-roles), el más antiguo pasa a Dueño y el resto a Vendedor.
            var primerMiembro = membersSinRol[0];
            var dueñoId = yaTieneDueno ? (Guid?)null : primerMiembro.Id;

            foreach (var member in membersSinRol)
            {
                var user = await userManager.FindByIdAsync(member.Id.ToString());
                if (user is null) continue;
                var role = member.Id == dueñoId ? Roles.Dueno : Roles.Vendedor;
                await userManager.AddToRoleAsync(user, role);
            }

            logger.LogInformation(
                "Roles de tenant sembrados para {TenantId}: {Count} usuario(s), dueño={Dueno}.",
                group.Key, membersSinRol.Count, dueñoId);
        }
    }
}
