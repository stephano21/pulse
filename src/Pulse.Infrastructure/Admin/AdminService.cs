using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pulse.Application.Abstractions;
using Pulse.Application.Admin;
using Pulse.Domain;
using Pulse.Infrastructure.Data;
using Pulse.Infrastructure.Identity;

namespace Pulse.Infrastructure.Admin;

public sealed class AdminService(PulseDbContext db, UserManager<ApplicationUser> userManager) : IAdminService
{
    public async Task<IReadOnlyList<AdminTenantDto>> ListTenantsAsync(CancellationToken ct)
    {
        return await db.Tenants
            .OrderBy(t => t.Name)
            .Select(t => new AdminTenantDto(t.Id, t.Name, t.CreatedAt, db.Users.Count(u => u.TenantId == t.Id)))
            .ToListAsync(ct);
    }

    public async Task<AdminTenantDto?> GetTenantAsync(Guid tenantId, CancellationToken ct)
    {
        return await db.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => new AdminTenantDto(t.Id, t.Name, t.CreatedAt, db.Users.Count(u => u.TenantId == t.Id)))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<AdminTenantDto> CreateTenantAsync(string name, CancellationToken ct)
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = name.Trim(), CreatedAt = DateTimeOffset.UtcNow };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);
        return new AdminTenantDto(tenant.Id, tenant.Name, tenant.CreatedAt, 0);
    }

    public async Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(Guid? tenantId, CancellationToken ct)
    {
        var users = await (
                from u in db.Users
                where tenantId == null || u.TenantId == tenantId
                join t in db.Tenants on u.TenantId equals t.Id into tenantJoin
                from t in tenantJoin.DefaultIfEmpty()
                select new { User = u, TenantName = t != null ? t.Name : null })
            .ToListAsync(ct);

        var rolesByUserId = (await (
                from ur in db.UserRoles
                join r in db.Roles on ur.RoleId equals r.Id
                select new { ur.UserId, r.Name })
            .ToListAsync(ct))
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Name!).ToList());

        return users
            .OrderBy(x => x.User.Email)
            .Select(x => new AdminUserDto(
                x.User.Id,
                x.User.Email!,
                x.User.EmailConfirmed,
                x.User.TenantId,
                x.TenantName,
                rolesByUserId.TryGetValue(x.User.Id, out var roles) ? roles : Array.Empty<string>(),
                x.User.LastLoginAt,
                x.User.CreatedAt))
            .ToList();
    }

    public async Task<bool> SetSuperAdminAsync(Guid userId, bool enabled, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return false;

        var isSuperAdmin = await userManager.IsInRoleAsync(user, Roles.SuperAdmin);
        if (enabled == isSuperAdmin)
            return true;

        if (!enabled)
        {
            var superAdmins = await userManager.GetUsersInRoleAsync(Roles.SuperAdmin);
            if (superAdmins.Count <= 1)
                return false;

            await userManager.RemoveFromRoleAsync(user, Roles.SuperAdmin);
        }
        else
        {
            await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
        }

        return true;
    }
}
