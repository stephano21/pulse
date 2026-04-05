using Pulse.Application.Sync;

namespace Pulse.Application.Abstractions;

public interface ISyncService
{
    Task<SyncBatchResponse> PushProductosAsync(Guid tenantId, ProductosSyncRequest request, CancellationToken ct);
    Task<SyncBatchResponse> PushClientesAsync(Guid tenantId, ClientesSyncRequest request, CancellationToken ct);
    Task<SyncBatchResponse> PushVentasAsync(Guid tenantId, VentasSyncRequest request, CancellationToken ct);
    Task<SyncBatchResponse> PushCobrosAsync(Guid tenantId, CobrosSyncRequest request, CancellationToken ct);
    Task<PagedProductosResponse> PullProductosAsync(Guid tenantId, DateTimeOffset? updatedSince, string? cursor, int limit, CancellationToken ct);
    Task<PagedClientesResponse> PullClientesAsync(Guid tenantId, DateTimeOffset? updatedSince, string? cursor, int limit, CancellationToken ct);
}
