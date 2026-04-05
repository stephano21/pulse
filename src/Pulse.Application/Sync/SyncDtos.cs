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
