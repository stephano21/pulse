using Microsoft.EntityFrameworkCore;
using Pulse.Application.Abstractions;
using Pulse.Application.Sync;
using Pulse.Domain;
using Pulse.Infrastructure.Data;

namespace Pulse.Infrastructure.Sync;

public sealed class SyncService(PulseDbContext db) : ISyncService
{
    private const string EntityProducto = "producto";
    private const string EntityCliente = "cliente";
    private const string EntityVenta = "venta";
    private const string EntityCobro = "cobro";

    public async Task<SyncBatchResponse> PushProductosAsync(Guid tenantId, ProductosSyncRequest request, CancellationToken ct)
    {
        var results = new List<SyncResultItem>();
        foreach (var item in request.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.MutationId))
            {
                var dup = await FindMutationAsync(tenantId, item.MutationId, EntityProducto, item.LocalId, ct);
                if (dup != null)
                {
                    results.Add(new SyncResultItem { LocalId = item.LocalId, RemoteId = dup.Value, Status = "duplicate" });
                    continue;
                }
            }

            var mapping = await db.ProductLocalMappings
                .Include(m => m.Product)
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.LocalId == item.LocalId, ct);

            if (mapping?.Product != null)
            {
                var p = mapping.Product;
                p.Nombre = item.Nombre;
                p.PrecioVenta = item.PrecioVenta;
                p.PrecioCosto = item.PrecioCosto;
                p.PrecioMinimo = item.PrecioMinimo;
                p.Stock = item.Stock;
                p.UpdatedAt = ClockMax(p.UpdatedAt, item.ClientUpdatedAt);
                await db.SaveChangesAsync(ct);
                await RegisterMutationIfAnyAsync(tenantId, item.MutationId, EntityProducto, item.LocalId, p.Id, "updated", ct);
                results.Add(new SyncResultItem { LocalId = item.LocalId, RemoteId = p.Id, Status = "updated" });
                continue;
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Nombre = item.Nombre,
                PrecioVenta = item.PrecioVenta,
                PrecioCosto = item.PrecioCosto,
                PrecioMinimo = item.PrecioMinimo,
                Stock = item.Stock,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = item.ClientUpdatedAt
            };
            db.Products.Add(product);
            db.ProductLocalMappings.Add(new ProductLocalMapping
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                LocalId = item.LocalId,
                ProductId = product.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ct);
            await RegisterMutationIfAnyAsync(tenantId, item.MutationId, EntityProducto, item.LocalId, product.Id, "created", ct);
            results.Add(new SyncResultItem { LocalId = item.LocalId, RemoteId = product.Id, Status = "created" });
        }

        return new SyncBatchResponse { Results = results };
    }

    public async Task<SyncBatchResponse> PushClientesAsync(Guid tenantId, ClientesSyncRequest request, CancellationToken ct)
    {
        var results = new List<SyncResultItem>();
        foreach (var item in request.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.MutationId))
            {
                var dup = await FindMutationAsync(tenantId, item.MutationId, EntityCliente, item.LocalId, ct);
                if (dup != null)
                {
                    results.Add(new SyncResultItem { LocalId = item.LocalId, RemoteId = dup.Value, Status = "duplicate" });
                    continue;
                }
            }

            var mapping = await db.ClienteLocalMappings
                .Include(m => m.Cliente)
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.LocalId == item.LocalId, ct);

            if (mapping?.Cliente != null)
            {
                var c = mapping.Cliente;
                c.Nombre = item.Nombre;
                c.DeudaInicial = item.DeudaInicial;
                c.SaldoAFavor = item.SaldoAFavor;
                c.UpdatedAt = ClockMax(c.UpdatedAt, item.ClientUpdatedAt);
                await db.SaveChangesAsync(ct);
                await RegisterMutationIfAnyAsync(tenantId, item.MutationId, EntityCliente, item.LocalId, c.Id, "updated", ct);
                results.Add(new SyncResultItem { LocalId = item.LocalId, RemoteId = c.Id, Status = "updated" });
                continue;
            }

            var cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Nombre = item.Nombre,
                DeudaInicial = item.DeudaInicial,
                SaldoAFavor = item.SaldoAFavor,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = item.ClientUpdatedAt
            };
            db.Clientes.Add(cliente);
            db.ClienteLocalMappings.Add(new ClienteLocalMapping
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                LocalId = item.LocalId,
                ClienteId = cliente.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ct);
            await RegisterMutationIfAnyAsync(tenantId, item.MutationId, EntityCliente, item.LocalId, cliente.Id, "created", ct);
            results.Add(new SyncResultItem { LocalId = item.LocalId, RemoteId = cliente.Id, Status = "created" });
        }

        return new SyncBatchResponse { Results = results };
    }

    public async Task<SyncBatchResponse> PushVentasAsync(Guid tenantId, VentasSyncRequest request, CancellationToken ct)
    {
        var results = new List<SyncResultItem>();
        foreach (var item in request.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.MutationId))
            {
                var dup = await FindMutationAsync(tenantId, item.MutationId, EntityVenta, item.LocalId, ct);
                if (dup != null)
                {
                    results.Add(new SyncResultItem { LocalId = item.LocalId, RemoteId = dup.Value, Status = "duplicate" });
                    continue;
                }
            }

            var existingMap = await db.VentaLocalMappings
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.LocalId == item.LocalId, ct);
            if (existingMap != null)
            {
                results.Add(new SyncResultItem { LocalId = item.LocalId, RemoteId = existingMap.VentaId, Status = "duplicate" });
                continue;
            }

            if (item.Estado == EstadoVenta.fiado)
            {
                var cid = await ResolveClienteIdAsync(tenantId, item.ClienteId, item.ClienteLocalId, ct);
                if (cid == null)
                    throw new InvalidOperationException($"Venta fiado requiere cliente_id o cliente_local_id válido (local_id={item.LocalId}).");
            }

            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var ventaId = Guid.NewGuid();
                var clienteResolved = await ResolveClienteIdAsync(tenantId, item.ClienteId, item.ClienteLocalId, ct);

                var venta = new Venta
                {
                    Id = ventaId,
                    TenantId = tenantId,
                    Fecha = item.Fecha,
                    Total = item.Total,
                    MetodoPago = item.MetodoPago,
                    Estado = item.Estado,
                    ClienteId = clienteResolved,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                db.Ventas.Add(venta);

                foreach (var line in item.LineItems)
                {
                    var productoId = await ResolveProductoIdAsync(tenantId, line.ProductoRemoteId, line.ProductoLocalId, ct);
                    db.VentaLineas.Add(new VentaLinea
                    {
                        Id = Guid.NewGuid(),
                        VentaId = ventaId,
                        Descripcion = line.Descripcion,
                        Cantidad = line.Cantidad,
                        PrecioUnitario = line.PrecioUnitario,
                        Subtotal = line.Subtotal,
                        ProductoId = productoId
                    });
                }

                db.VentaLocalMappings.Add(new VentaLocalMapping
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    LocalId = item.LocalId,
                    VentaId = ventaId,
                    CreatedAt = DateTimeOffset.UtcNow
                });

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                await RegisterMutationIfAnyAsync(tenantId, item.MutationId, EntityVenta, item.LocalId, ventaId, "created", ct);
                results.Add(new SyncResultItem { LocalId = item.LocalId, RemoteId = ventaId, Status = "created" });
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        return new SyncBatchResponse { Results = results };
    }

    public async Task<SyncBatchResponse> PushCobrosAsync(Guid tenantId, CobrosSyncRequest request, CancellationToken ct)
    {
        var results = new List<SyncResultItem>();
        foreach (var item in request.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.MutationId))
            {
                var dup = await FindMutationAsync(tenantId, item.MutationId, EntityCobro, item.LocalId, ct);
                if (dup != null)
                {
                    results.Add(new SyncResultItem { LocalId = item.LocalId, RemoteId = dup.Value, Status = "duplicate" });
                    continue;
                }
            }

            var existingMap = await db.CobroLocalMappings
                .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.LocalId == item.LocalId, ct);
            if (existingMap != null)
            {
                results.Add(new SyncResultItem { LocalId = item.LocalId, RemoteId = existingMap.CobroId, Status = "duplicate" });
                continue;
            }

            var clienteExists = await db.Clientes.AnyAsync(c => c.TenantId == tenantId && c.Id == item.ClienteId, ct);
            if (!clienteExists)
                throw new InvalidOperationException($"Cliente {item.ClienteId} no existe en el tenant.");

            var cobro = new Cobro
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ClienteId = item.ClienteId,
                Monto = item.Monto,
                Fecha = item.Fecha,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Cobros.Add(cobro);
            db.CobroLocalMappings.Add(new CobroLocalMapping
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                LocalId = item.LocalId,
                CobroId = cobro.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ct);
            await RegisterMutationIfAnyAsync(tenantId, item.MutationId, EntityCobro, item.LocalId, cobro.Id, "created", ct);
            results.Add(new SyncResultItem { LocalId = item.LocalId, RemoteId = cobro.Id, Status = "created" });
        }

        return new SyncBatchResponse { Results = results };
    }

    public async Task<PagedProductosResponse> PullProductosAsync(Guid tenantId, DateTimeOffset? updatedSince, string? cursor, int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, 500);
        var q = db.Products.AsNoTracking().Where(p => p.TenantId == tenantId && p.DeletedAt == null);

        if (CursorHelper.TryDecode(cursor, out var cur) && cur != null)
        {
            var u = cur.UpdatedAt;
            var id = cur.Id;
            q = q.Where(p => p.UpdatedAt > u || (p.UpdatedAt == u && p.Id > id));
        }
        else if (updatedSince.HasValue)
        {
            q = q.Where(p => p.UpdatedAt > updatedSince.Value);
        }

        var rows = await q
            .OrderBy(p => p.UpdatedAt).ThenBy(p => p.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        string? next = null;
        if (rows.Count > limit)
        {
            var last = rows[limit - 1];
            next = CursorHelper.Encode(last.UpdatedAt, last.Id);
            rows = rows.Take(limit).ToList();
        }

        var productIds = rows.Select(r => r.Id).ToList();
        var localIds = new Dictionary<Guid, long>();
        if (productIds.Count > 0)
        {
            localIds = await db.ProductLocalMappings.AsNoTracking()
                .Where(m => m.TenantId == tenantId && productIds.Contains(m.ProductId))
                .ToDictionaryAsync(m => m.ProductId, m => m.LocalId, ct);
        }

        var items = rows.Select(p => new ProductoDto
        {
            Id = p.Id,
            LocalId = localIds.TryGetValue(p.Id, out var plid) ? plid : null,
            Nombre = p.Nombre,
            PrecioVenta = p.PrecioVenta,
            PrecioCosto = p.PrecioCosto,
            PrecioMinimo = p.PrecioMinimo,
            Stock = p.Stock,
            UpdatedAt = p.UpdatedAt
        }).ToList();

        return new PagedProductosResponse { Items = items, NextCursor = next };
    }

    public async Task<PagedClientesResponse> PullClientesAsync(Guid tenantId, DateTimeOffset? updatedSince, string? cursor, int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, 500);
        var q = db.Clientes.AsNoTracking().Where(c => c.TenantId == tenantId);

        if (CursorHelper.TryDecode(cursor, out var cur) && cur != null)
        {
            var u = cur.UpdatedAt;
            var id = cur.Id;
            q = q.Where(c => c.UpdatedAt > u || (c.UpdatedAt == u && c.Id > id));
        }
        else if (updatedSince.HasValue)
        {
            q = q.Where(c => c.UpdatedAt > updatedSince.Value);
        }

        var rows = await q
            .OrderBy(c => c.UpdatedAt).ThenBy(c => c.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        string? next = null;
        if (rows.Count > limit)
        {
            var last = rows[limit - 1];
            next = CursorHelper.Encode(last.UpdatedAt, last.Id);
            rows = rows.Take(limit).ToList();
        }

        var clienteIds = rows.Select(r => r.Id).ToList();
        var localIds = new Dictionary<Guid, long>();
        if (clienteIds.Count > 0)
        {
            localIds = await db.ClienteLocalMappings.AsNoTracking()
                .Where(m => m.TenantId == tenantId && clienteIds.Contains(m.ClienteId))
                .ToDictionaryAsync(m => m.ClienteId, m => m.LocalId, ct);
        }

        var items = rows.Select(c => new ClienteDto
        {
            Id = c.Id,
            LocalId = localIds.TryGetValue(c.Id, out var clid) ? clid : null,
            Nombre = c.Nombre,
            DeudaInicial = c.DeudaInicial,
            SaldoAFavor = c.SaldoAFavor,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        return new PagedClientesResponse { Items = items, NextCursor = next };
    }

    private async Task<Guid?> FindMutationAsync(Guid tenantId, string mutationId, string entityType, long localId, CancellationToken ct)
    {
        var row = await db.ProcessedMutations.AsNoTracking()
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.MutationId == mutationId && m.EntityType == entityType, ct);
        if (row == null) return null;
        if (row.LocalId != localId)
            throw new InvalidOperationException("mutation_id reutilizado con distinto local_id.");
        return row.RemoteId;
    }

    private async Task RegisterMutationIfAnyAsync(Guid tenantId, string? mutationId, string entityType, long localId, Guid remoteId, string status, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(mutationId)) return;
        var mid = mutationId.Trim();
        if (await db.ProcessedMutations.AnyAsync(m => m.TenantId == tenantId && m.MutationId == mid && m.EntityType == entityType, ct))
            return;
        db.ProcessedMutations.Add(new ProcessedMutation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MutationId = mid,
            EntityType = entityType,
            LocalId = localId,
            RemoteId = remoteId,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task<Guid?> ResolveClienteIdAsync(Guid tenantId, Guid? remote, long? localId, CancellationToken ct)
    {
        if (remote.HasValue)
        {
            var ok = await db.Clientes.AnyAsync(c => c.TenantId == tenantId && c.Id == remote.Value, ct);
            return ok ? remote : null;
        }
        if (localId.HasValue)
        {
            var m = await db.ClienteLocalMappings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.LocalId == localId.Value, ct);
            return m?.ClienteId;
        }
        return null;
    }

    private async Task<Guid?> ResolveProductoIdAsync(Guid tenantId, Guid? remote, long? localId, CancellationToken ct)
    {
        if (remote.HasValue)
        {
            var ok = await db.Products.AnyAsync(p => p.TenantId == tenantId && p.Id == remote.Value && p.DeletedAt == null, ct);
            return ok ? remote : null;
        }
        if (localId.HasValue)
        {
            var m = await db.ProductLocalMappings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.LocalId == localId.Value, ct);
            return m?.ProductId;
        }
        return null;
    }

    private static DateTimeOffset ClockMax(DateTimeOffset a, DateTimeOffset b) => a >= b ? a : b;
}
