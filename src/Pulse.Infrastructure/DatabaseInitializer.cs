using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pulse.Domain;
using Pulse.Infrastructure.Data;

namespace Pulse.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task MigrateAndSeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        if (!await db.Tenants.AnyAsync(cancellationToken))
        {
            var defaultId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            db.Tenants.Add(new Tenant { Id = defaultId, Name = "default", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
