using Pulse.Application.Admin;
using Pulse.Application.Sync;

namespace Pulse.Application.Abstractions;

public interface IAdminService
{
    Task<IReadOnlyList<AdminTenantDto>> ListTenantsAsync(CancellationToken ct);
    Task<AdminTenantDto?> GetTenantAsync(Guid tenantId, CancellationToken ct);
    Task<AdminTenantDto> CreateTenantAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(Guid? tenantId, CancellationToken ct);

    /// <summary>Promueve/revoca SuperAdmin. Devuelve false si la operación dejaría el sistema sin ningún SuperAdmin.</summary>
    Task<bool> SetSuperAdminAsync(Guid userId, bool enabled, CancellationToken ct);

    /// <summary>Corrige manualmente el stock de un producto. Devuelve null si el producto no existe en el tenant.</summary>
    Task<ProductoDto?> AdjustProductoStockAsync(Guid tenantId, Guid productoId, int stock, CancellationToken ct);

    /// <summary>Corrige manualmente deuda_inicial/saldo_a_favor de un cliente. Devuelve null si no existe en el tenant.</summary>
    Task<ClienteDto?> AdjustClienteSaldoAsync(Guid tenantId, Guid clienteId, decimal deudaInicial, decimal saldoAFavor, CancellationToken ct);

    /// <summary>
    /// Registra una venta a nombre del tenant (ej: soporte telefónico) y descuenta el stock de cada
    /// línea con producto asociado, igual que haría el propio negocio desde la app.
    /// </summary>
    Task<VentaDto> CreateVentaAsync(Guid tenantId, AdminCreateVentaRequest request, CancellationToken ct);
}
