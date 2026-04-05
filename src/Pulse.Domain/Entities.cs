namespace Pulse.Domain;

public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
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
