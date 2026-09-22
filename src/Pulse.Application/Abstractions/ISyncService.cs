using Pulse.Application.Sync;

namespace Pulse.Application.Abstractions;

public interface ISyncService
{
    Task<SyncBatchResponse> PushProductosAsync(Guid tenantId, ProductosSyncRequest request, CancellationToken ct);
    Task<SyncBatchResponse> PushClientesAsync(Guid tenantId, ClientesSyncRequest request, CancellationToken ct);
    /// <summary>`vendedorId` queda como dueño de cada venta creada (Venta.VendedorId) — quien la registró desde la app.</summary>
    Task<SyncBatchResponse> PushVentasAsync(Guid tenantId, Guid vendedorId, VentasSyncRequest request, CancellationToken ct);
    Task<SyncBatchResponse> PushCobrosAsync(Guid tenantId, CobrosSyncRequest request, CancellationToken ct);
    Task<SyncBatchResponse> PushUnidadesAsync(Guid tenantId, UnidadesSyncRequest request, CancellationToken ct);
    Task<SyncBatchResponse> PushProveedoresAsync(Guid tenantId, ProveedoresSyncRequest request, CancellationToken ct);
    Task<SyncBatchResponse> PushComprasProveedorAsync(Guid tenantId, ComprasProveedorSyncRequest request, CancellationToken ct);
    Task<SyncBatchResponse> PushPagosProveedorAsync(Guid tenantId, PagosProveedorSyncRequest request, CancellationToken ct);
    Task<PagedProductosResponse> PullProductosAsync(Guid tenantId, DateTimeOffset? updatedSince, string? cursor, int limit, CancellationToken ct);
    Task<PagedClientesResponse> PullClientesAsync(Guid tenantId, DateTimeOffset? updatedSince, string? cursor, int limit, CancellationToken ct);
    /// <summary>
    /// `scopeToVendedorId` no nulo = solo devuelve las ventas de ese vendedor + las fiadas de todo
    /// el tenant (compartidas). Null = todo el tenant, con VendedorEmail para atribución (Dueño/
    /// Gerente/SuperAdmin).
    /// </summary>
    Task<PagedVentasResponse> PullVentasAsync(Guid tenantId, Guid? scopeToVendedorId, DateTimeOffset? createdSince, string? cursor, int limit, CancellationToken ct);
    Task<PagedCobrosResponse> PullCobrosAsync(Guid tenantId, DateTimeOffset? createdSince, string? cursor, int limit, CancellationToken ct);
    Task<PagedUnidadesResponse> PullUnidadesAsync(Guid tenantId, DateTimeOffset? updatedSince, string? cursor, int limit, CancellationToken ct);
    Task<PagedProveedoresResponse> PullProveedoresAsync(Guid tenantId, DateTimeOffset? updatedSince, string? cursor, int limit, CancellationToken ct);
    Task<PagedComprasProveedorResponse> PullComprasProveedorAsync(Guid tenantId, DateTimeOffset? createdSince, string? cursor, int limit, CancellationToken ct);
    Task<PagedPagosProveedorResponse> PullPagosProveedorAsync(Guid tenantId, DateTimeOffset? createdSince, string? cursor, int limit, CancellationToken ct);
}
