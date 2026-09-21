namespace Pulse.Domain;

public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Reply-To (y nombre de marca en el From) para los correos relacionados a este tenant.
    /// Null = usa el remitente genérico de la app (Email:FromName/FromAddress del .env).
    /// No es una cuenta SMTP propia: los correos se siguen mandando autenticados con la
    /// cuenta única configurada en el servidor, esto solo cambia a dónde llegan las respuestas.
    /// </summary>
    public string? NotificationEmail { get; set; }

    /// <summary>Referencia a <see cref="StoredFile"/> — el archivo se sube antes por separado (POST /v1/files).</summary>
    public Guid? LogoFileId { get; set; }
}

/// <summary>
/// Tabla genérica de archivos subidos al storage: sin relación fija a ningún tipo de entidad
/// (Tenant, ApplicationUser, etc. simplemente guardan un Guid que apunta acá). Flujo: subir a
/// POST /v1/files → queda con Id propio → recién ahí se asocia (ej. PUT /v1/team/tenant/logo).
/// </summary>
public sealed class StoredFile
{
    public Guid Id { get; set; }

    /// <summary>Key del objeto en el bucket (no una URL): el bucket es privado, la URL se firma al vuelo.</summary>
    public string Key { get; set; } = "";

    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }

    /// <summary>Tenant dueño/alcance del archivo (para poder validar pertenencia al asociarlo). Null = archivo de plataforma.</summary>
    public Guid? TenantId { get; set; }

    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class Product
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string Nombre { get; set; } = "";
    public decimal PrecioVenta { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal? PrecioMinimo { get; set; }
    public int Stock { get; set; }

    /// <summary>Referencia a <see cref="StoredFile"/> — el archivo se sube antes por separado (POST /v1/files).</summary>
    public Guid? ImagenFileId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public sealed class ProductLocalMapping
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public long LocalId { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class Cliente
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string Nombre { get; set; } = "";
    public decimal DeudaInicial { get; set; }
    public decimal SaldoAFavor { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public sealed class ClienteLocalMapping
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public long LocalId { get; set; }
    public Guid ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class Venta
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public decimal Total { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public EstadoVenta Estado { get; set; }
    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<VentaLinea> Lineas { get; set; } = new List<VentaLinea>();
}

public sealed class VentaLinea
{
    public Guid Id { get; set; }
    public Guid VentaId { get; set; }
    public Venta? Venta { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public Guid? ProductoId { get; set; }
    public Product? Producto { get; set; }
}

public sealed class VentaLocalMapping
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public long LocalId { get; set; }
    public Guid VentaId { get; set; }
    public Venta? Venta { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class Cobro
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public decimal Monto { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CobroLocalMapping
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public long LocalId { get; set; }
    public Guid CobroId { get; set; }
    public Cobro? Cobro { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Proveedor del negocio. Su saldo (lo que se le debe) = DeudaInicial + compras a crédito − pagos.
/// </summary>
public sealed class Proveedor
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string Nombre { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Notas { get; set; }

    /// <summary>Lo que ya se le debía al proveedor antes de empezar a usar la app.</summary>
    public decimal DeudaInicial { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public sealed class ProveedorLocalMapping
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public long LocalId { get; set; }
    public Guid ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Compra a crédito al proveedor: aumenta lo que se le debe, no mueve caja hasta que se pague.</summary>
public sealed class CompraProveedor
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public decimal Monto { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public string? Nota { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CompraProveedorLocalMapping
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public long LocalId { get; set; }
    public Guid CompraId { get; set; }
    public CompraProveedor? Compra { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Pago hecho a un proveedor (sale plata). Es el que se resta del total vendido del día para
/// obtener el neto. Si fue por transferencia puede llevar la foto del comprobante
/// (<see cref="ComprobanteFileId"/> apunta a <see cref="StoredFile"/>, subido antes por POST /v1/files).
/// </summary>
public sealed class PagoProveedor
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public decimal Monto { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public string? Nota { get; set; }
    public Guid? ComprobanteFileId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class PagoProveedorLocalMapping
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public long LocalId { get; set; }
    public Guid PagoId { get; set; }
    public PagoProveedor? Pago { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class IdempotencyRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Key { get; set; } = "";
    public string RequestPath { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public string ResponseBody { get; set; } = "";
    public int StatusCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ProcessedMutation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string MutationId { get; set; } = "";
    public string EntityType { get; set; } = "";
    public long LocalId { get; set; }
    public Guid RemoteId { get; set; }
    public string Status { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class UnidadMedida
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string Nombre { get; set; } = "";
    public int Unidades { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public sealed class UnidadMedidaLocalMapping
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public long LocalId { get; set; }
    public Guid UnidadId { get; set; }
    public UnidadMedida? Unidad { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>SHA-256 hex del valor crudo entregado al cliente; el valor crudo nunca se persiste.</summary>
    public string TokenHash { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Hash del token que lo reemplazó al rotar (auditoría de la cadena de rotación).</summary>
    public string? ReplacedByTokenHash { get; set; }

    public string? CreatedByIp { get; set; }
}
