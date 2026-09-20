using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pulse.Application.Abstractions;
using Pulse.Application.Admin;
using Pulse.Application.Sync;
using Pulse.Domain;
using Pulse.Infrastructure.Data;
using Pulse.Infrastructure.Identity;

namespace Pulse.Infrastructure.Admin;

public sealed class AdminService(PulseDbContext db, UserManager<ApplicationUser> userManager, IFileStorageService storage) : IAdminService
{
    private async Task<string?> ResolveLogoUrlAsync(Guid? logoFileId, CancellationToken ct)
    {
        if (!logoFileId.HasValue || !storage.IsConfigured)
            return null;

        var key = await db.Files.Where(f => f.Id == logoFileId.Value).Select(f => f.Key).FirstOrDefaultAsync(ct);
        return key is null ? null : storage.GetPresignedUrl(key);
    }

    private async Task<AdminTenantDto> ToDtoAsync(Tenant tenant, int userCount, CancellationToken ct) =>
        new(tenant.Id, tenant.Name, tenant.CreatedAt, userCount, tenant.NotificationEmail, await ResolveLogoUrlAsync(tenant.LogoFileId, ct));

    public async Task<IReadOnlyList<AdminTenantDto>> ListTenantsAsync(CancellationToken ct)
    {
        var tenants = await db.Tenants.OrderBy(t => t.Name).ToListAsync(ct);

        var logoIds = tenants.Where(t => t.LogoFileId.HasValue).Select(t => t.LogoFileId!.Value).Distinct().ToList();
        var keysById = logoIds.Count > 0 && storage.IsConfigured
            ? await db.Files.Where(f => logoIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.Key, ct)
            : new Dictionary<Guid, string>();

        var result = new List<AdminTenantDto>();
        foreach (var t in tenants)
        {
            var userCount = await db.Users.CountAsync(u => u.TenantId == t.Id, ct);
            var logoUrl = t.LogoFileId.HasValue && keysById.TryGetValue(t.LogoFileId.Value, out var key)
                ? storage.GetPresignedUrl(key)
                : null;
            result.Add(new AdminTenantDto(t.Id, t.Name, t.CreatedAt, userCount, t.NotificationEmail, logoUrl));
        }
        return result;
    }

    public async Task<AdminTenantDto?> GetTenantAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null)
            return null;

        var userCount = await db.Users.CountAsync(u => u.TenantId == tenant.Id, ct);
        return await ToDtoAsync(tenant, userCount, ct);
    }

    public async Task<AdminTenantDto> CreateTenantAsync(string name, CancellationToken ct)
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = name.Trim(), CreatedAt = DateTimeOffset.UtcNow };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);
        return new AdminTenantDto(tenant.Id, tenant.Name, tenant.CreatedAt, 0, null, null);
    }

    public async Task<AdminTenantDto?> UpdateTenantAsync(Guid tenantId, string name, string? notificationEmail, CancellationToken ct)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null)
            return null;

        tenant.Name = name.Trim();
        tenant.NotificationEmail = string.IsNullOrWhiteSpace(notificationEmail) ? null : notificationEmail.Trim();
        await db.SaveChangesAsync(ct);

        var userCount = await db.Users.CountAsync(u => u.TenantId == tenant.Id, ct);
        return await ToDtoAsync(tenant, userCount, ct);
    }

    public async Task<AdminTenantDto?> SetTenantLogoAsync(Guid tenantId, Guid fileId, CancellationToken ct)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null)
            return null;

        var file = await db.Files.FirstOrDefaultAsync(f => f.Id == fileId, ct);
        if (file is null || file.TenantId != tenantId)
            throw new InvalidOperationException("El archivo no existe o no pertenece a este tenant.");

        tenant.LogoFileId = fileId;
        await db.SaveChangesAsync(ct);

        var userCount = await db.Users.CountAsync(u => u.TenantId == tenant.Id, ct);
        return await ToDtoAsync(tenant, userCount, ct);
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
                x.User.CreatedAt,
                x.User.LockoutEnd == null || x.User.LockoutEnd <= DateTimeOffset.UtcNow))
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

    public async Task<bool> SetEmailConfirmedAsync(Guid userId, bool confirmed, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return false;

        if (user.EmailConfirmed == confirmed)
            return true;

        user.EmailConfirmed = confirmed;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<AdminUserDto> CreateTeamUserAsync(Guid tenantId, string email, string password, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            TenantId = tenantId,
            AuthProvider = AuthProviders.Local,
            CreatedAt = now
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));

        var tenantName = await db.Tenants.Where(t => t.Id == tenantId).Select(t => t.Name).FirstOrDefaultAsync(ct);
        return new AdminUserDto(user.Id, user.Email!, user.EmailConfirmed, user.TenantId, tenantName, [], user.LastLoginAt, user.CreatedAt, true);
    }

    public async Task<bool> SetUserActiveAsync(Guid userId, bool active, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return false;

        if (!active && await userManager.IsInRoleAsync(user, Roles.SuperAdmin))
        {
            var superAdmins = await userManager.GetUsersInRoleAsync(Roles.SuperAdmin);
            if (superAdmins.Count <= 1)
                return false;
        }

        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, active ? null : DateTimeOffset.MaxValue);
        return true;
    }

    public async Task<AdminUserDto?> MoveUserToTenantAsync(Guid userId, Guid tenantId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return null;

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null)
            return null;

        user.TenantId = tenantId;
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        return new AdminUserDto(user.Id, user.Email!, user.EmailConfirmed, user.TenantId, tenant.Name, roles.ToArray(), user.LastLoginAt, user.CreatedAt, await userManager.IsLockedOutAsync(user) == false);
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return false;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));

        return true;
    }

    public async Task<ProductoDto?> AdjustProductoStockAsync(Guid tenantId, Guid productoId, int stock, CancellationToken ct)
    {
        var producto = await db.Products
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == productoId && p.DeletedAt == null, ct);
        if (producto is null)
            return null;

        producto.Stock = stock;
        producto.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var localId = await db.ProductLocalMappings.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.ProductId == producto.Id)
            .Select(m => (long?)m.LocalId)
            .FirstOrDefaultAsync(ct);

        return new ProductoDto
        {
            Id = producto.Id,
            LocalId = localId,
            Nombre = producto.Nombre,
            PrecioVenta = producto.PrecioVenta,
            PrecioCosto = producto.PrecioCosto,
            PrecioMinimo = producto.PrecioMinimo,
            Stock = producto.Stock,
            UpdatedAt = producto.UpdatedAt
        };
    }

    public async Task<ClienteDto?> AdjustClienteSaldoAsync(Guid tenantId, Guid clienteId, decimal deudaInicial, decimal saldoAFavor, CancellationToken ct)
    {
        var cliente = await db.Clientes
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == clienteId && c.DeletedAt == null, ct);
        if (cliente is null)
            return null;

        cliente.DeudaInicial = deudaInicial;
        cliente.SaldoAFavor = saldoAFavor;
        cliente.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var localId = await db.ClienteLocalMappings.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.ClienteId == cliente.Id)
            .Select(m => (long?)m.LocalId)
            .FirstOrDefaultAsync(ct);

        return new ClienteDto
        {
            Id = cliente.Id,
            LocalId = localId,
            Nombre = cliente.Nombre,
            DeudaInicial = cliente.DeudaInicial,
            SaldoAFavor = cliente.SaldoAFavor,
            UpdatedAt = cliente.UpdatedAt
        };
    }

    public async Task<VentaDto> CreateVentaAsync(Guid tenantId, AdminCreateVentaRequest request, CancellationToken ct)
    {
        if (request.Lineas.Count == 0)
            throw new InvalidOperationException("La venta debe tener al menos una línea.");

        if (request.Estado == EstadoVenta.fiado && request.ClienteId is null)
            throw new InvalidOperationException("Venta fiado requiere cliente_id.");

        if (request.ClienteId.HasValue)
        {
            var clienteExiste = await db.Clientes.AnyAsync(
                c => c.TenantId == tenantId && c.Id == request.ClienteId.Value && c.DeletedAt == null, ct);
            if (!clienteExiste)
                throw new InvalidOperationException("El cliente no existe en el tenant.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var lineas = new List<VentaLinea>();
        decimal total = 0;

        foreach (var line in request.Lineas)
        {
            if (line.ProductoId.HasValue)
            {
                var producto = await db.Products.FirstOrDefaultAsync(
                    p => p.TenantId == tenantId && p.Id == line.ProductoId.Value && p.DeletedAt == null, ct);
                if (producto is null)
                    throw new InvalidOperationException($"El producto {line.ProductoId} no existe en el tenant.");

                // Stock es entero pero cantidad admite fracciones (venta por peso, etc.); se redondea
                // al entero más cercano para descontar, igual de aproximado que el resto del dominio.
                var cantidadEntera = (int)Math.Round(line.Cantidad, MidpointRounding.AwayFromZero);
                if (producto.Stock - cantidadEntera < 0)
                    throw new InvalidOperationException($"Stock insuficiente para \"{producto.Nombre}\" (disponible: {producto.Stock}).");

                producto.Stock -= cantidadEntera;
                producto.UpdatedAt = DateTimeOffset.UtcNow;
            }

            var subtotal = line.Cantidad * line.PrecioUnitario;
            total += subtotal;
            lineas.Add(new VentaLinea
            {
                Id = Guid.NewGuid(),
                Descripcion = line.Descripcion,
                Cantidad = line.Cantidad,
                PrecioUnitario = line.PrecioUnitario,
                Subtotal = subtotal,
                ProductoId = line.ProductoId
            });
        }

        var venta = new Venta
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Fecha = request.Fecha ?? DateTimeOffset.UtcNow,
            Total = total,
            MetodoPago = request.MetodoPago,
            Estado = request.Estado,
            ClienteId = request.ClienteId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        foreach (var linea in lineas)
            linea.VentaId = venta.Id;

        db.Ventas.Add(venta);
        db.VentaLineas.AddRange(lineas);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new VentaDto
        {
            Id = venta.Id,
            LocalId = null,
            Fecha = venta.Fecha,
            Total = venta.Total,
            MetodoPago = venta.MetodoPago,
            Estado = venta.Estado,
            ClienteId = venta.ClienteId,
            CreatedAt = venta.CreatedAt,
            Lineas = lineas.Select(l => new VentaLineaDto
            {
                Id = l.Id,
                Descripcion = l.Descripcion,
                Cantidad = l.Cantidad,
                PrecioUnitario = l.PrecioUnitario,
                Subtotal = l.Subtotal,
                ProductoId = l.ProductoId
            }).ToList()
        };
    }
}
