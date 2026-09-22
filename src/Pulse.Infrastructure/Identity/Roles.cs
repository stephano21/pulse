namespace Pulse.Infrastructure.Identity;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>Control total sobre su propio tenant: config, vendedores, ajustes manuales, reportes completos.</summary>
    public const string Dueno = "Dueno";

    /// <summary>Solo lectura sobre su propio tenant: reportes sin costos/ganancia, sin gestión de vendedores ni ajustes.</summary>
    public const string Gerente = "Gerente";

    /// <summary>Rol de la app móvil: solo ve sus propias ventas cobradas (lo fiado se comparte con el resto del equipo).</summary>
    public const string Vendedor = "Vendedor";

    /// <summary>Roles con acceso de lectura/gestión sobre su propio tenant en /v1/team (todo menos Vendedor).</summary>
    public const string TenantAdminRoles = $"{Dueno},{Gerente}";
}
