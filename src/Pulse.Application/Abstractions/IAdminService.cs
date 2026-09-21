using Pulse.Application.Admin;
using Pulse.Application.Sync;

namespace Pulse.Application.Abstractions;

public interface IAdminService
{
    Task<IReadOnlyList<AdminTenantDto>> ListTenantsAsync(CancellationToken ct);
    Task<AdminTenantDto?> GetTenantAsync(Guid tenantId, CancellationToken ct);
    Task<AdminTenantDto> CreateTenantAsync(string name, CancellationToken ct);
    Task<AdminTenantDto?> UpdateTenantAsync(Guid tenantId, string name, string? notificationEmail, CancellationToken ct);

    /// <summary>
    /// Asocia un archivo ya subido (POST /v1/files) como logo del tenant. Devuelve null si el
    /// tenant no existe; lanza InvalidOperationException si el archivo no existe o es de otro tenant.
    /// </summary>
    Task<AdminTenantDto?> SetTenantLogoAsync(Guid tenantId, Guid fileId, CancellationToken ct);

    /// <summary>Como SetTenantLogoAsync, pero sin exigir que el archivo pertenezca al tenant (lo usa /v1/admin, el SuperAdmin gestiona cualquier tenant).</summary>
    Task<AdminTenantDto?> SetTenantLogoAdminAsync(Guid tenantId, Guid fileId, CancellationToken ct);

    /// <summary>
    /// Borra un tenant SOLO si no tiene usuarios (pensado para limpiar huérfanos de registros que
    /// fallaron a mitad de camino, no para negocios reales con datos). Null = no existe;
    /// false = existe pero tiene usuarios, no se borró; true = borrado.
    /// </summary>
    Task<bool?> DeleteTenantAsync(Guid tenantId, CancellationToken ct);

    Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(Guid? tenantId, CancellationToken ct);

    /// <summary>
    /// Crea un usuario dentro de un tenant existente, con el correo ya confirmado (lo da de alta
    /// alguien que ya pertenece al tenant, así que no hace falta el flujo de confirmación por
    /// correo). Usado por POST /v1/team/users. Lanza InvalidOperationException si Identity
    /// rechaza el alta (correo duplicado, contraseña débil, etc.).
    /// </summary>
    Task<AdminUserDto> CreateTeamUserAsync(Guid tenantId, string email, string password, CancellationToken ct);

    /// <summary>Promueve/revoca SuperAdmin. Devuelve false si la operación dejaría el sistema sin ningún SuperAdmin.</summary>
    Task<bool> SetSuperAdminAsync(Guid userId, bool enabled, CancellationToken ct);

    /// <summary>Marca/desmarca el correo como confirmado a mano (soporte). Devuelve false si el usuario no existe.</summary>
    Task<bool> SetEmailConfirmedAsync(Guid userId, bool confirmed, CancellationToken ct);

    /// <summary>
    /// Activa/desactiva el acceso del usuario (lockout indefinido de Identity — no puede iniciar
    /// sesión ni refrescar el token mientras esté inactivo). Devuelve false si el usuario no existe
    /// o si esta acción dejaría el sistema sin ningún SuperAdmin.
    /// </summary>
    Task<bool> SetUserActiveAsync(Guid userId, bool active, CancellationToken ct);

    /// <summary>Mueve un usuario a otro tenant. Devuelve null si el usuario o el tenant destino no existen.</summary>
    Task<AdminUserDto?> MoveUserToTenantAsync(Guid userId, Guid tenantId, CancellationToken ct);

    /// <summary>
    /// Restablece la contraseña de un usuario a mano (soporte: perdió el correo, quedó trabado, etc.).
    /// Devuelve false si el usuario no existe. Lanza InvalidOperationException si la contraseña no
    /// cumple la política (se mapea a 400).
    /// </summary>
    Task<bool> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct);

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
