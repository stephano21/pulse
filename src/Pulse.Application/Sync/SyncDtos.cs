using System.Text.Json.Serialization;
using Pulse.Domain;

namespace Pulse.Application.Sync;

public sealed class SyncResultItem
{
    public long LocalId { get; set; }
    public Guid RemoteId { get; set; }
    public string Status { get; set; } = "";
}

public sealed class SyncBatchResponse
{
    public List<SyncResultItem> Results { get; set; } = [];
}

public sealed class ProductoSyncItem
{
    public long LocalId { get; set; }
    public string? MutationId { get; set; }
    public string Nombre { get; set; } = "";
    public decimal PrecioVenta { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal? PrecioMinimo { get; set; }
    public int Stock { get; set; }
    public DateTimeOffset ClientUpdatedAt { get; set; }
    public bool Deleted { get; set; }
}

public sealed class ProductosSyncRequest
{
    public List<ProductoSyncItem> Items { get; set; } = [];
}

public sealed class ClienteSyncItem
{
    public long LocalId { get; set; }
    public string? MutationId { get; set; }
    public string Nombre { get; set; } = "";
    public decimal DeudaInicial { get; set; }
    public decimal SaldoAFavor { get; set; }
    public DateTimeOffset ClientUpdatedAt { get; set; }
    public bool Deleted { get; set; }
}

public sealed class ClientesSyncRequest
{
    public List<ClienteSyncItem> Items { get; set; } = [];
}

public sealed class VentaLineaSyncItem
{
    public string Descripcion { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public long? ProductoLocalId { get; set; }
    public Guid? ProductoRemoteId { get; set; }
}

public sealed class VentaSyncItem
{
    public long LocalId { get; set; }
    public string? MutationId { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public decimal Total { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MetodoPago MetodoPago { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EstadoVenta Estado { get; set; }

    public Guid? ClienteId { get; set; }
    public long? ClienteLocalId { get; set; }
    public List<VentaLineaSyncItem> LineItems { get; set; } = [];
}

public sealed class VentasSyncRequest
{
    public List<VentaSyncItem> Items { get; set; } = [];
}

public sealed class CobroSyncItem
{
    public long LocalId { get; set; }
    public string? MutationId { get; set; }
    public Guid ClienteId { get; set; }
    public decimal Monto { get; set; }
    public DateTimeOffset Fecha { get; set; }
}

public sealed class CobrosSyncRequest
{
    public List<CobroSyncItem> Items { get; set; } = [];
}

public sealed class ProductoDto
{
    public Guid Id { get; set; }
    public long? LocalId { get; set; }
    public string Nombre { get; set; } = "";
    public decimal PrecioVenta { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal? PrecioMinimo { get; set; }
    public int Stock { get; set; }
    public Guid? ImagenFileId { get; set; }
    /// <summary>URL firmada temporal (bucket privado) — null si el producto no tiene imagen o el storage no está configurado.</summary>
    public string? ImagenUrl { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PagedProductosResponse
{
    public List<ProductoDto> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}

public sealed class ClienteDto
{
    public Guid Id { get; set; }
    public long? LocalId { get; set; }
    public string Nombre { get; set; } = "";
    public decimal DeudaInicial { get; set; }
    public decimal SaldoAFavor { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PagedClientesResponse
{
    public List<ClienteDto> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}

public sealed class VentaLineaDto
{
    public Guid Id { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public Guid? ProductoId { get; set; }
    /// <summary>No nulo = esta línea reversa (en negativo) esta línea de la venta original.</summary>
    public Guid? ReversaDeVentaLineaId { get; set; }
}

public sealed class VentaDto
{
    public Guid Id { get; set; }
    public long? LocalId { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public decimal Total { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MetodoPago MetodoPago { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EstadoVenta Estado { get; set; }

    public Guid? ClienteId { get; set; }
    public Guid? VendedorId { get; set; }
    /// <summary>Solo para quien puede ver todo el tenant (Dueño/Gerente/SuperAdmin) — null en la vista de un Vendedor.</summary>
    public string? VendedorEmail { get; set; }
    /// <summary>No nulo = esta venta ES un reverso de la venta con este Id.</summary>
    public Guid? ReversaDeVentaId { get; set; }
    public string? MotivoReverso { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<VentaLineaDto> Lineas { get; set; } = [];
}

public sealed class ReversoLineaItem
{
    public Guid VentaLineaId { get; set; }
    public decimal Cantidad { get; set; }
}

public sealed class ReversarVentaRequest
{
    /// <summary>Vacío/null = reversa todo lo que quede pendiente de cada línea (reverso completo).</summary>
    public List<ReversoLineaItem>? Lineas { get; set; }
    public string? Motivo { get; set; }
}

public sealed class PagedVentasResponse
{
    public List<VentaDto> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}

public sealed class CobroDto
{
    public Guid Id { get; set; }
    public long? LocalId { get; set; }
    public Guid ClienteId { get; set; }
    public decimal Monto { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class PagedCobrosResponse
{
    public List<CobroDto> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}

public sealed class UnidadSyncItem
{
    public long LocalId { get; set; }
    public string? MutationId { get; set; }
    public string Nombre { get; set; } = "";
    public int Unidades { get; set; }
    public DateTimeOffset ClientUpdatedAt { get; set; }
    public bool Deleted { get; set; }
}

public sealed class UnidadesSyncRequest
{
    public List<UnidadSyncItem> Items { get; set; } = [];
}

public sealed class UnidadDto
{
    public Guid Id { get; set; }
    public long? LocalId { get; set; }
    public string Nombre { get; set; } = "";
    public int Unidades { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PagedUnidadesResponse
{
    public List<UnidadDto> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}

public sealed class ProveedorSyncItem
{
    public long LocalId { get; set; }
    public string? MutationId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Notas { get; set; }
    public decimal DeudaInicial { get; set; }
    public DateTimeOffset ClientUpdatedAt { get; set; }
    public bool Deleted { get; set; }
}

public sealed class ProveedoresSyncRequest
{
    public List<ProveedorSyncItem> Items { get; set; } = [];
}

public sealed class CompraProveedorSyncItem
{
    public long LocalId { get; set; }
    public string? MutationId { get; set; }
    public Guid ProveedorId { get; set; }
    public decimal Monto { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public string? Nota { get; set; }
}

public sealed class ComprasProveedorSyncRequest
{
    public List<CompraProveedorSyncItem> Items { get; set; } = [];
}

public sealed class PagoProveedorSyncItem
{
    public long LocalId { get; set; }
    public string? MutationId { get; set; }
    public Guid ProveedorId { get; set; }
    public decimal Monto { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MetodoPago MetodoPago { get; set; }

    public DateTimeOffset Fecha { get; set; }
    public string? Nota { get; set; }

    /// <summary>Foto del comprobante de transferencia, ya subida por POST /v1/files.</summary>
    public Guid? ComprobanteFileId { get; set; }
}

public sealed class PagosProveedorSyncRequest
{
    public List<PagoProveedorSyncItem> Items { get; set; } = [];
}

public sealed class ProveedorDto
{
    public Guid Id { get; set; }
    public long? LocalId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Notas { get; set; }
    public decimal DeudaInicial { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PagedProveedoresResponse
{
    public List<ProveedorDto> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}

public sealed class CompraProveedorDto
{
    public Guid Id { get; set; }
    public long? LocalId { get; set; }
    public Guid ProveedorId { get; set; }
    public decimal Monto { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public string? Nota { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class PagedComprasProveedorResponse
{
    public List<CompraProveedorDto> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}

public sealed class PagoProveedorDto
{
    public Guid Id { get; set; }
    public long? LocalId { get; set; }
    public Guid ProveedorId { get; set; }
    public decimal Monto { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MetodoPago MetodoPago { get; set; }

    public DateTimeOffset Fecha { get; set; }
    public string? Nota { get; set; }
    public Guid? ComprobanteFileId { get; set; }

    /// <summary>URL firmada y temporal de la foto del comprobante (null si no hay o el storage no está configurado).</summary>
    public string? ComprobanteUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class PagedPagosProveedorResponse
{
    public List<PagoProveedorDto> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}
