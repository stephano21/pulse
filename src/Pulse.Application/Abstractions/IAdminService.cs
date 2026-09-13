using Pulse.Application.Admin;

namespace Pulse.Application.Abstractions;

public interface IAdminService
{
    Task<IReadOnlyList<AdminTenantDto>> ListTenantsAsync(CancellationToken ct);
    Task<AdminTenantDto?> GetTenantAsync(Guid tenantId, CancellationToken ct);
    Task<AdminTenantDto> CreateTenantAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(Guid? tenantId, CancellationToken ct);

    /// <summary>Promueve/revoca SuperAdmin. Devuelve false si la operación dejaría el sistema sin ningún SuperAdmin.</summary>
    Task<bool> SetSuperAdminAsync(Guid userId, bool enabled, CancellationToken ct);
}
