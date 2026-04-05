using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pulse.Domain;
using Pulse.Infrastructure.Identity;

namespace Pulse.Infrastructure.Data;

public sealed class PulseDbContext(DbContextOptions<PulseDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductLocalMapping> ProductLocalMappings => Set<ProductLocalMapping>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ClienteLocalMapping> ClienteLocalMappings => Set<ClienteLocalMapping>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaLinea> VentaLineas => Set<VentaLinea>();
    public DbSet<VentaLocalMapping> VentaLocalMappings => Set<VentaLocalMapping>();
    public DbSet<Cobro> Cobros => Set<Cobro>();
    public DbSet<CobroLocalMapping> CobroLocalMappings => Set<CobroLocalMapping>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<ProcessedMutation> ProcessedMutations => Set<ProcessedMutation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.ToTable("users", DatabaseSchemas.Identity);
            e.HasIndex(u => u.TenantId);
            e.Property(u => u.AuthProvider).HasMaxLength(32).HasDefaultValue(AuthProviders.Local);
            e.Property(u => u.GoogleSubject).HasMaxLength(256);
            e.HasIndex(u => u.GoogleSubject).IsUnique();
            e.Property(u => u.ProfilePictureUrl).HasMaxLength(2048);
            e.HasOne<Tenant>().WithMany().HasForeignKey(u => u.TenantId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<IdentityRole<Guid>>(e => e.ToTable("roles", DatabaseSchemas.Identity));
        modelBuilder.Entity<IdentityUserRole<Guid>>(e => e.ToTable("user_roles", DatabaseSchemas.Identity));
        modelBuilder.Entity<IdentityUserClaim<Guid>>(e => e.ToTable("user_claims", DatabaseSchemas.Identity));
        modelBuilder.Entity<IdentityUserLogin<Guid>>(e => e.ToTable("user_logins", DatabaseSchemas.Identity));
        modelBuilder.Entity<IdentityRoleClaim<Guid>>(e => e.ToTable("role_claims", DatabaseSchemas.Identity));
        modelBuilder.Entity<IdentityUserToken<Guid>>(e => e.ToTable("user_tokens", DatabaseSchemas.Identity));

        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("tenants", DatabaseSchemas.Pulse);
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products", DatabaseSchemas.Pulse);
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(512);
            e.Property(x => x.PrecioVenta).HasPrecision(18, 4);
            e.Property(x => x.PrecioCosto).HasPrecision(18, 4);
            e.Property(x => x.PrecioMinimo).HasPrecision(18, 4);
            e.HasIndex(x => new { x.TenantId, x.UpdatedAt });
            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductLocalMapping>(e =>
        {
            e.ToTable("product_local_mappings", DatabaseSchemas.Pulse);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.LocalId }).IsUnique();
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("clientes", DatabaseSchemas.Pulse);
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(512);
            e.Property(x => x.DeudaInicial).HasPrecision(18, 4);
            e.Property(x => x.SaldoAFavor).HasPrecision(18, 4);
            e.HasIndex(x => new { x.TenantId, x.UpdatedAt });
            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClienteLocalMapping>(e =>
        {
            e.ToTable("cliente_local_mappings", DatabaseSchemas.Pulse);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.LocalId }).IsUnique();
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Venta>(e =>
        {
            e.ToTable("ventas", DatabaseSchemas.Pulse);
            e.HasKey(x => x.Id);
            e.Property(x => x.Total).HasPrecision(18, 4);
            e.Property(x => x.MetodoPago).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(x => new { x.TenantId, x.Fecha });
            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VentaLinea>(e =>
        {
            e.ToTable("venta_lineas", DatabaseSchemas.Pulse);
            e.HasKey(x => x.Id);
            e.Property(x => x.Descripcion).HasMaxLength(512);
            e.Property(x => x.Cantidad).HasPrecision(18, 4);
            e.Property(x => x.PrecioUnitario).HasPrecision(18, 4);
            e.Property(x => x.Subtotal).HasPrecision(18, 4);
            e.HasOne(x => x.Venta).WithMany(v => v.Lineas).HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VentaLocalMapping>(e =>
        {
            e.ToTable("venta_local_mappings", DatabaseSchemas.Pulse);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.LocalId }).IsUnique();
            e.HasOne(x => x.Venta).WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Cobro>(e =>
        {
            e.ToTable("cobros", DatabaseSchemas.Pulse);
            e.HasKey(x => x.Id);
            e.Property(x => x.Monto).HasPrecision(18, 4);
            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CobroLocalMapping>(e =>
        {
            e.ToTable("cobro_local_mappings", DatabaseSchemas.Pulse);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.LocalId }).IsUnique();
            e.HasOne(x => x.Cobro).WithMany().HasForeignKey(x => x.CobroId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IdempotencyRecord>(e =>
        {
            e.ToTable("idempotency_records", DatabaseSchemas.Pulse);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.Key, x.RequestPath }).IsUnique();
            e.Property(x => x.Key).HasMaxLength(128);
            e.Property(x => x.RequestPath).HasMaxLength(512);
            e.Property(x => x.RequestHash).HasMaxLength(64);
        });

        modelBuilder.Entity<ProcessedMutation>(e =>
        {
            e.ToTable("processed_mutations", DatabaseSchemas.Pulse);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.MutationId, x.EntityType }).IsUnique();
            e.Property(x => x.MutationId).HasMaxLength(128);
            e.Property(x => x.EntityType).HasMaxLength(32);
            e.Property(x => x.Status).HasMaxLength(32);
        });
    }
}
