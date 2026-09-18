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
