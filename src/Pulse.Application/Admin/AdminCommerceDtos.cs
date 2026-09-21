using Pulse.Domain;

namespace Pulse.Application.Admin;

public sealed class AdjustProductoStockRequest
{
    public int Stock { get; set; }
}

public sealed class AdjustClienteSaldoRequest
{
    public decimal DeudaInicial { get; set; }
    public decimal SaldoAFavor { get; set; }
}

public sealed class AdminVentaLineaRequest
{
    public string Descripcion { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public Guid? ProductoId { get; set; }
}

public sealed class AdminCreateVentaRequest
{
    public DateTimeOffset? Fecha { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public EstadoVenta Estado { get; set; }
    public Guid? ClienteId { get; set; }
    public List<AdminVentaLineaRequest> Lineas { get; set; } = [];
}

public sealed class AdminCreateProveedorRequest
{
    public string Nombre { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Notas { get; set; }
    public decimal DeudaInicial { get; set; }
}

public sealed class AdminCreateCompraProveedorRequest
{
    public Guid ProveedorId { get; set; }
    public decimal Monto { get; set; }
    public DateTimeOffset? Fecha { get; set; }
    public string? Nota { get; set; }
}

public sealed class AdminCreatePagoProveedorRequest
{
    public Guid ProveedorId { get; set; }
    public decimal Monto { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public DateTimeOffset? Fecha { get; set; }
    public string? Nota { get; set; }

    /// <summary>Foto del comprobante (transferencia), ya subida por POST /v1/files.</summary>
    public Guid? ComprobanteFileId { get; set; }
}
