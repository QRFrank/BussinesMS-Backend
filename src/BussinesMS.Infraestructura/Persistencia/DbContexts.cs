using BussinesMS.Dominio.Entidades.Auth;
using BussinesMS.Dominio.Entidades.Sistema;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Persistencia;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> opciones) : base(opciones)
    {
    }

    public DbSet<Sistema> Sistemas => Set<Sistema>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Almacen> Almacenes => Set<Almacen>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<UsuarioMenu> UsuarioMenus => Set<UsuarioMenu>();
    public DbSet<UsuarioSistema> UsuarioSistemas => Set<UsuarioSistema>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Sistema>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MenuIds).HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Apellido).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();

            entity.HasOne(u => u.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(u => u.RolId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Almacen>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.Property(e => e.SistemaId).HasDefaultValue(1);

            entity.HasOne(a => a.Sistema)
                .WithMany()
                .HasForeignKey(a => a.SistemaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UsuarioSistema>(entity =>
        {
            entity.HasKey(e => new { e.UsuarioId, e.SistemaId });

            entity.HasOne(us => us.Usuario)
                .WithMany(u => u.UsuarioSistemas)
                .HasForeignKey(us => us.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(us => us.Sistema)
                .WithMany()
                .HasForeignKey(us => us.SistemaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Url).HasMaxLength(200);
            entity.Property(e => e.Icono).HasMaxLength(100);

            entity.HasOne(m => m.Parent)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Sistema)
                .WithMany()
                .HasForeignKey(m => m.SistemaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UsuarioMenu>(entity =>
        {
            entity.HasKey(e => new { e.UsuarioId, e.MenuId });

            entity.HasOne(um => um.Usuario)
                .WithMany(u => u.UsuarioMenus)
                .HasForeignKey(um => um.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(um => um.Menu)
                .WithMany()
                .HasForeignKey(um => um.MenuId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.PermisosEspeciales)
                .HasColumnType("nvarchar(max)");
        });
    }
}

public class SistemaDbContext : DbContext
{
    public SistemaDbContext(DbContextOptions<SistemaDbContext> opciones) : base(opciones)
    {
    }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Fabricante> Fabricantes => Set<Fabricante>();
    public DbSet<DescripcionSabor> DescripcionSabores => Set<DescripcionSabor>();
    public DbSet<DescripcionTamanio> DescripcionTamanios => Set<DescripcionTamanio>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<ProductoVariante> ProductoVariantes => Set<ProductoVariante>();
    public DbSet<TipoPresentacion> TiposPresentacion => Set<TipoPresentacion>();
    public DbSet<ProductoPresentacion> ProductoPresentaciones => Set<ProductoPresentacion>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraDetalle> CompraDetalles => Set<CompraDetalle>();
    public DbSet<PagoCompra> PagosCompra => Set<PagoCompra>();
    public DbSet<InventarioLote> InventarioLotes => Set<InventarioLote>();
    public DbSet<InventarioLoteAlmacen> InventarioLoteAlmacenes => Set<InventarioLoteAlmacen>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<DevolucionCliente> DevolucionesClientes => Set<DevolucionCliente>();
    public DbSet<SesionCaja> SesionesCaja => Set<SesionCaja>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaDetalle> VentaDetalles => Set<VentaDetalle>();
    public DbSet<CategoriaGasto> CategoriasGasto => Set<CategoriaGasto>();
    public DbSet<GastoOperativo> GastosOperativos => Set<GastoOperativo>();
    public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<Fabricante>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CodigoInterno).HasMaxLength(20);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.CodigoInterno).IsUnique();
            entity.HasIndex(e => e.Nombre);

            entity.HasOne(p => p.Categoria)
                .WithMany()
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Fabricante)
                .WithMany()
                .HasForeignKey(p => p.FabricanteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductoVariante>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CodigoBarras).HasMaxLength(50);
            entity.Property(e => e.PrecioVentaUnitario).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.PrecioVentaMayoreo).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.PrecioCompra).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.CodigoAlmacen).HasMaxLength(50);

            entity.HasIndex(e => e.CodigoBarras).IsUnique().HasFilter("[CodigoBarras] IS NOT NULL");
            entity.HasIndex(e => new { e.ProductoId, e.SaborId, e.TamanioId })
                  .IsUnique()
                  .HasDatabaseName("UQ_Variante_Combinacion");

            entity.HasOne(pv => pv.Producto)
                .WithMany()
                .HasForeignKey(pv => pv.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(pv => pv.Sabor)
                .WithMany()
                .HasForeignKey(pv => pv.SaborId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(pv => pv.Tamanio)
                .WithMany()
                .HasForeignKey(pv => pv.TamanioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TipoPresentacion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.HasIndex(e => e.Orden).IsUnique();
        });

        modelBuilder.Entity<ProductoPresentacion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NombrePersonalizado).HasMaxLength(50);
            entity.Property(e => e.CodigoBarras).HasMaxLength(50);
            entity.HasIndex(e => e.CodigoBarras)
                  .IsUnique()
                  .HasFilter("[CodigoBarras] IS NOT NULL AND [IsActive] = 1");

            entity.HasIndex(e => new { e.VarianteId, e.TipoPresentacionId })
                  .IsUnique()
                  .HasFilter("[IsActive] = 1")
                  .HasDatabaseName("UQ_Presentacion_Variante");

            entity.HasIndex(e => e.VarianteId)
                  .IsUnique()
                  .HasFilter("[EsDefaultReporte] = 1 AND [IsActive] = 1")
                  .HasDatabaseName("UQ_Presentacion_DefaultReporte");

            entity.HasOne(pp => pp.Variante)
                .WithMany(pv => pv.Presentaciones)
                .HasForeignKey(pp => pp.VarianteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pp => pp.TipoPresentacion)
                .WithMany(tp => tp.Presentaciones)
                .HasForeignKey(pp => pp.TipoPresentacionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(pp => pp.PresentacionPadre)
                .WithMany(pp => pp.PresentacionesHijas)
                .HasForeignKey(pp => pp.PresentacionPadreId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Nit).HasMaxLength(20);
            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(e => e.NumeroCarnet).HasMaxLength(20);
            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.HasIndex(e => e.NumeroCarnet).IsUnique().HasFilter("[NumeroCarnet] IS NOT NULL");

            // Seed del cliente genérico ("Sin nombre") — valores estáticos para no generar diffs en cada migración
            entity.HasData(new Cliente
            {
                Id = Cliente.ClienteGenericoId,
                Nombre = "Sin nombre",
                NumeroCarnet = null,
                Telefono = null,
                IsActive = true,
                CreatedAt = new DateTime(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = null,
                DeletedAt = null,
                CreatedByUsuarioId = 1,
                UpdatedByUsuarioId = null,
                DeletedByUsuarioId = null
            });
        });

        modelBuilder.Entity<Compra>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalCompra).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Observacion).HasMaxLength(500);

            entity.HasOne(c => c.Proveedor)
                .WithMany()
                .HasForeignKey(c => c.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CompraDetalle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CostoUnitario).IsRequired().HasColumnType("decimal(18,4)");
            entity.Property(e => e.Subtotal).IsRequired().HasColumnType("decimal(18,2)");

            entity.HasOne(d => d.Compra)
                .WithMany(c => c.Detalles)
                .HasForeignKey(d => d.CompraId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Variante)
                .WithMany()
                .HasForeignKey(d => d.VarianteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PagoCompra>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Monto).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.MontoCaja).IsRequired().HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(e => e.MontoExterno).IsRequired().HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(e => e.Observacion).HasMaxLength(255);
            entity.Property(e => e.PagadoPorUsuarioId).IsRequired();

            entity.HasOne(p => p.Compra)
                .WithMany(c => c.Pagos)
                .HasForeignKey(p => p.CompraId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =============================================
        // INVENTARIO — InventarioLote (IDENTIDAD)
        // =============================================
        modelBuilder.Entity<InventarioLote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CodigoLote).HasMaxLength(60).IsRequired();
            entity.Property(e => e.CostoCompraUnitario).IsRequired().HasColumnType("decimal(18,4)");
            entity.Property(e => e.StockInicial).IsRequired();
            entity.Property(e => e.CantidadVendida).IsRequired();
            entity.Property(e => e.CantidadVencida).IsRequired();
            entity.Property(e => e.EstadoLote).IsRequired();
            entity.Property(e => e.FechaVencimiento).HasColumnType("date");

            entity.HasIndex(e => e.CodigoLote).IsUnique().HasDatabaseName("UQ_InventarioLote_CodigoLote");

            entity.HasOne(l => l.Variante)
                .WithMany()
                .HasForeignKey(l => l.VarianteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.CompraDetalle)
                .WithMany()
                .HasForeignKey(l => l.CompraDetalleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // =============================================
        // INVENTARIO — InventarioLoteAlmacen (STOCK/UBICACIÓN)
        // =============================================
        modelBuilder.Entity<InventarioLoteAlmacen>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.LoteId, e.AlmacenId })
                  .IsUnique()
                  .HasDatabaseName("UQ_LoteAlmacen");

            entity.HasIndex(e => new { e.AlmacenId, e.LoteId })
                  .HasDatabaseName("IX_LoteAlmacen_FEFO");

            entity.HasIndex(e => new { e.VarianteId, e.AlmacenId })
                  .HasDatabaseName("IX_LoteAlmacen_VarianteAlmacen");

            entity.HasOne(la => la.Lote)
                .WithMany()
                .HasForeignKey(la => la.LoteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =============================================
        // MOVIMIENTOS — MovimientoInventario
        // =============================================
        modelBuilder.Entity<MovimientoInventario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Observacion).HasMaxLength(255);
            entity.Property(e => e.FechaMovimiento).HasColumnType("datetime2");

            entity.HasIndex(e => e.LoteAlmacenId);
            entity.HasIndex(e => e.VarianteId);
            entity.HasIndex(e => new { e.AlmacenOrigenId, e.AlmacenDestinoId });

            entity.HasOne(m => m.LoteAlmacen)
                .WithMany()
                .HasForeignKey(m => m.LoteAlmacenId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Variante)
                .WithMany()
                .HasForeignKey(m => m.VarianteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =============================================
        // DEVOLUCIONES — DevolucionCliente
        // =============================================
        modelBuilder.Entity<DevolucionCliente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Motivo).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Observacion).HasMaxLength(255);
            entity.Property(e => e.FechaDevolucion).HasColumnType("datetime2");

            entity.HasOne(d => d.LoteAlmacenOrigen)
                .WithMany()
                .HasForeignKey(d => d.LoteAlmacenOrigenId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.LoteAlmacenDevuelto)
                .WithMany()
                .HasForeignKey(d => d.LoteAlmacenDevueltoId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Variante)
                .WithMany()
                .HasForeignKey(d => d.VarianteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =============================================
        // VENTAS Y CAJA — SesionCaja
        // =============================================
        modelBuilder.Entity<SesionCaja>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MontoInicial).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.IngresosEfectivo).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.IngresosDigitales).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.EgresosGastos).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.EgresosPagoProveedor).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.MontoEsperadoEfectivo).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MontoRealEntregado).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Diferencia).HasColumnType("decimal(18,2)");

            entity.HasIndex(e => new { e.UsuarioId, e.AlmacenId, e.Estado })
                  .HasDatabaseName("IX_SesionCaja_Usuario_Almacen");
        });

        // =============================================
        // VENTAS Y CAJA — Venta
        // =============================================
        modelBuilder.Entity<Venta>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalBruto).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.DescuentoTotal).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalNeto).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.MotivoDescuento).HasMaxLength(255);
            entity.Property(e => e.MontoEfectivo).IsRequired().HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(e => e.MontoTransferencia).IsRequired().HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(e => e.MontoRecibido).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Cambio).HasColumnType("decimal(18,2)");

            entity.HasIndex(e => e.SesionCajaId);
            entity.HasIndex(e => e.FechaVenta);

            entity.HasOne(v => v.SesionCaja)
                .WithMany(s => s.Ventas)
                .HasForeignKey(v => v.SesionCajaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cliente — default 1 (cliente genérico) para ventas existentes
            entity.Property(e => e.ClienteId).HasDefaultValue(Cliente.ClienteGenericoId);
            entity.HasIndex(e => e.ClienteId);

            entity.HasOne(v => v.Cliente)
                .WithMany()
                .HasForeignKey(v => v.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =============================================
        // VENTAS Y CAJA — VentaDetalle
        // =============================================
        modelBuilder.Entity<VentaDetalle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrecioUnitarioCobrado).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.CostoUnitarioLote).IsRequired().HasColumnType("decimal(18,4)");
            entity.Property(e => e.Subtotal).IsRequired().HasColumnType("decimal(18,2)");
            // Nullable a propósito: ventas históricas quedan sin registrar (null), sin backfill.
            entity.Property(e => e.TipoPrecio);

            entity.HasIndex(e => e.VentaId);
            entity.HasIndex(e => e.LoteId);

            entity.HasOne(d => d.Venta)
                .WithMany(v => v.Detalles)
                .HasForeignKey(d => d.VentaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Variante)
                .WithMany()
                .HasForeignKey(d => d.VarianteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Lote)
                .WithMany()
                .HasForeignKey(d => d.LoteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =============================================
        // VENTAS Y CAJA — CategoriaGasto
        // =============================================
        modelBuilder.Entity<CategoriaGasto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        // =============================================
        // VENTAS Y CAJA — GastoOperativo
        // =============================================
        modelBuilder.Entity<GastoOperativo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Monto).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.MontoCaja).IsRequired().HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(e => e.MontoExterno).IsRequired().HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            // Default SQL para que las filas existentes queden con fecha válida
            entity.Property(e => e.FechaGasto).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.SesionCajaId);
            entity.HasIndex(e => e.FechaGasto);
            entity.HasIndex(e => e.AlmacenId);

            // SesionCaja opcional: null = gasto externo (fuera de caja)
            entity.HasOne(g => g.SesionCaja)
                .WithMany()
                .HasForeignKey(g => g.SesionCajaId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(g => g.CategoriaGasto)
                .WithMany()
                .HasForeignKey(g => g.CategoriaGastoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public class NavidadDbContext : DbContext
{
    public NavidadDbContext(DbContextOptions<NavidadDbContext> opciones) : base(opciones)
    {
    }

    public DbSet<BussinesMS.Dominio.Entidades.Navidad.Temporada> Temporadas => Set<BussinesMS.Dominio.Entidades.Navidad.Temporada>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.Inversor> Inversores => Set<BussinesMS.Dominio.Entidades.Navidad.Inversor>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.AporteCapital> AportesCapital => Set<BussinesMS.Dominio.Entidades.Navidad.AporteCapital>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.PagoInversor> PagosInversor => Set<BussinesMS.Dominio.Entidades.Navidad.PagoInversor>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.CategoriaGastoNav> CategoriasGasto => Set<BussinesMS.Dominio.Entidades.Navidad.CategoriaGastoNav>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.CategoriaProductoNav> CategoriasProducto => Set<BussinesMS.Dominio.Entidades.Navidad.CategoriaProductoNav>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.GastoNav> Gastos => Set<BussinesMS.Dominio.Entidades.Navidad.GastoNav>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.TemporadaAlmacenConteo> TemporadaAlmacenesConteo => Set<BussinesMS.Dominio.Entidades.Navidad.TemporadaAlmacenConteo>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.Proveedor> Proveedores => Set<BussinesMS.Dominio.Entidades.Navidad.Proveedor>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.CodigoCliente> CodigosCliente => Set<BussinesMS.Dominio.Entidades.Navidad.CodigoCliente>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.Producto> Productos => Set<BussinesMS.Dominio.Entidades.Navidad.Producto>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.Vendedor> Vendedores => Set<BussinesMS.Dominio.Entidades.Navidad.Vendedor>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.ClienteNav> ClientesNav => Set<BussinesMS.Dominio.Entidades.Navidad.ClienteNav>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.Pedido> Pedidos => Set<BussinesMS.Dominio.Entidades.Navidad.Pedido>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.PedidoDetalle> PedidosDetalle => Set<BussinesMS.Dominio.Entidades.Navidad.PedidoDetalle>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.Recepcion> Recepciones => Set<BussinesMS.Dominio.Entidades.Navidad.Recepcion>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.RecepcionDetalle> RecepcionesDetalle => Set<BussinesMS.Dominio.Entidades.Navidad.RecepcionDetalle>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.RecepcionDistribucion> RecepcionesDistribucion => Set<BussinesMS.Dominio.Entidades.Navidad.RecepcionDistribucion>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.MovimientoNav> MovimientosNav => Set<BussinesMS.Dominio.Entidades.Navidad.MovimientoNav>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.PagoProveedor> PagosProveedor => Set<BussinesMS.Dominio.Entidades.Navidad.PagoProveedor>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.StockAlmacen> StocksAlmacen => Set<BussinesMS.Dominio.Entidades.Navidad.StockAlmacen>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.Compra> Compras => Set<BussinesMS.Dominio.Entidades.Navidad.Compra>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.CompraDetalle> ComprasDetalle => Set<BussinesMS.Dominio.Entidades.Navidad.CompraDetalle>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.CompraDistribucion> ComprasDistribucion => Set<BussinesMS.Dominio.Entidades.Navidad.CompraDistribucion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.Temporada>(entity =>
        {
            entity.ToTable("Temporada");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FechaInicio).HasColumnType("date");
            entity.Property(e => e.FechaCierre).HasColumnType("date");
            entity.Property(e => e.Estado).HasConversion<int>();
            // Garantiza en BD que solo exista una temporada abierta
            entity.HasIndex(e => e.Estado)
                .IsUnique()
                .HasFilter("[Estado] = 1")
                .HasDatabaseName("UX_Temporada_UnaAbierta");
            entity.HasIndex(e => e.Anio);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.TemporadaAlmacenConteo>(entity =>
        {
            entity.ToTable("TemporadaAlmacenConteo");
            entity.HasKey(e => new { e.TemporadaId, e.AlmacenId });
            // AlmacenId sin FK: Almacen vive en AuthDB
            entity.HasOne(e => e.Temporada)
                .WithMany(t => t.AlmacenesConteo)
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.AlmacenId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.Inversor>(entity =>
        {
            entity.ToTable("Inversor");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Documento).HasMaxLength(50);
            entity.Property(e => e.Telefono).HasMaxLength(50);
            entity.HasIndex(e => e.Nombre);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.AporteCapital>(entity =>
        {
            entity.ToTable("AporteCapital");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.PorcentajeComision).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            // InversorId null = capital propio
            entity.HasOne(e => e.Inversor)
                .WithMany()
                .HasForeignKey(e => e.InversorId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.TemporadaId);
            entity.HasIndex(e => e.InversorId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.PagoInversor>(entity =>
        {
            entity.ToTable("PagoInversor");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.Tipo).HasConversion<int>();
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AporteCapital)
                .WithMany(a => a.Pagos)
                .HasForeignKey(e => e.AporteCapitalId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.TemporadaId);
            entity.HasIndex(e => e.AporteCapitalId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.CategoriaGastoNav>(entity =>
        {
            entity.ToTable("CategoriaGastoNav");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            // Único también entre inactivos (igual que CategoriaGasto del regular)
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.GastoNav>(entity =>
        {
            entity.ToTable("GastoNav");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.Descripcion).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Categoria)
                .WithMany()
                .HasForeignKey(e => e.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.TemporadaId);
            entity.HasIndex(e => e.CategoriaId);
            entity.HasIndex(e => e.Fecha);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.CategoriaProductoNav>(entity =>
        {
            entity.ToTable("CategoriaProductoNav");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            // Único también entre inactivos (igual que CategoriaGastoNav)
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        // ===== Catálogo =====
        // Índices únicos filtrados por activos: permiten recrear tras borrado lógico
        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.Proveedor>(entity =>
        {
            entity.ToTable("Proveedor");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Telefono).HasMaxLength(50);
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.TemporadaId, e.Nombre })
                .IsUnique()
                .HasFilter("[IsActive] = 1")
                .HasDatabaseName("UX_Proveedor_Temporada_Nombre");
            entity.HasIndex(e => e.TemporadaId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.CodigoCliente>(entity =>
        {
            entity.ToTable("CodigoCliente");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Titular).IsRequired().HasMaxLength(150);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Proveedor)
                .WithMany(p => p.Codigos)
                .HasForeignKey(e => e.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ProveedorId, e.Codigo })
                .IsUnique()
                .HasFilter("[IsActive] = 1")
                .HasDatabaseName("UX_CodigoCliente_Proveedor_Codigo");
            entity.HasIndex(e => e.TemporadaId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.Producto>(entity =>
        {
            entity.ToTable("Producto");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Descripcion).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.PrecioCompraUnidad).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PrecioCatalogo).HasColumnType("decimal(18,2)");
            entity.Property(e => e.NombreEmpaque).HasMaxLength(20);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Proveedor)
                .WithMany(p => p.Productos)
                .HasForeignKey(e => e.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Categoria)
                .WithMany()
                .HasForeignKey(e => e.CategoriaProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ProveedorId, e.Descripcion })
                .IsUnique()
                .HasFilter("[IsActive] = 1")
                .HasDatabaseName("UX_Producto_Proveedor_Descripcion");
            entity.HasIndex(e => new { e.TemporadaId, e.Nombre })
                .IsUnique()
                .HasFilter("[Nombre] IS NOT NULL AND [IsActive] = 1")
                .HasDatabaseName("UX_Producto_Temporada_Nombre");
            entity.HasIndex(e => e.TemporadaId);
            entity.HasIndex(e => e.CategoriaProductoId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.Vendedor>(entity =>
        {
            entity.ToTable("Vendedor");
            entity.HasKey(e => e.Id);
            // UsuarioId sin FK: Usuario vive en AuthDB
            entity.Property(e => e.Tipo).HasConversion<int>();
            entity.Property(e => e.SueldoMensual).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.TemporadaId, e.UsuarioId })
                .IsUnique()
                .HasFilter("[IsActive] = 1")
                .HasDatabaseName("UX_Vendedor_Temporada_Usuario");
            entity.HasIndex(e => e.UsuarioId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.ClienteNav>(entity =>
        {
            entity.ToTable("ClienteNav");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Documento).HasMaxLength(50);
            entity.Property(e => e.Telefono).HasMaxLength(50);
            entity.Property(e => e.Direccion).HasMaxLength(250);
            entity.HasIndex(e => e.Documento)
                .IsUnique()
                .HasFilter("[Documento] IS NOT NULL AND [IsActive] = 1")
                .HasDatabaseName("UX_ClienteNav_Documento");
            entity.HasIndex(e => e.Nombre);
        });

        // ===== Abastecimiento =====
        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.Pedido>(entity =>
        {
            entity.ToTable("Pedido");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.MontoTotalProveedor).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Proveedor)
                .WithMany()
                .HasForeignKey(e => e.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CodigoCliente)
                .WithMany()
                .HasForeignKey(e => e.CodigoClienteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.TemporadaId);
            entity.HasIndex(e => e.ProveedorId);
            entity.HasIndex(e => e.CodigoClienteId);
            // Ajuste 2: un pedido por código (o por proveedor si no usa códigos) por temporada.
            // El proveedor ya es de una temporada; SQL Server trata los NULL como iguales (uno solo sin código).
            entity.HasIndex(e => new { e.ProveedorId, e.CodigoClienteId })
                .IsUnique()
                .HasFilter("[IsActive] = 1")
                .HasDatabaseName("UX_Pedido_Proveedor_Codigo");
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.PedidoDetalle>(entity =>
        {
            entity.ToTable("PedidoDetalle");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrecioCompraUnidad).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Pedido)
                .WithMany(p => p.Detalles)
                .HasForeignKey(e => e.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.PedidoId, e.ProductoId }).IsUnique();
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.Recepcion>(entity =>
        {
            entity.ToTable("Recepcion");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NroFactura).HasMaxLength(50);
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Proveedor)
                .WithMany()
                .HasForeignKey(e => e.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CodigoCliente)
                .WithMany()
                .HasForeignKey(e => e.CodigoClienteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.TemporadaId);
            entity.HasIndex(e => e.ProveedorId);
            entity.HasIndex(e => e.CodigoClienteId);
            entity.HasIndex(e => e.Fecha);
        });

        // Cada RecepcionDetalle es un LOTE
        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.RecepcionDetalle>(entity =>
        {
            entity.ToTable("RecepcionDetalle");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrecioCompraUnidad).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Recepcion)
                .WithMany(r => r.Detalles)
                .HasForeignKey(e => e.RecepcionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.ProductoId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.RecepcionDistribucion>(entity =>
        {
            entity.ToTable("RecepcionDistribucion");
            entity.HasKey(e => e.Id);
            // AlmacenId sin FK: Almacen vive en AuthDB
            entity.HasOne(e => e.RecepcionDetalle)
                .WithMany(d => d.Distribuciones)
                .HasForeignKey(e => e.RecepcionDetalleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.RecepcionDetalleId, e.AlmacenId }).IsUnique();
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.MovimientoNav>(entity =>
        {
            entity.ToTable("MovimientoNav");
            entity.HasKey(e => e.Id);
            // AlmacenId y UsuarioId sin FK: viven en AuthDB
            entity.Property(e => e.Tipo).HasConversion<int>();
            entity.Property(e => e.ReferenciaTipo).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Fecha).HasColumnType("datetime2");
            entity.Property(e => e.Motivo).HasMaxLength(500);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.AlmacenId, e.ProductoId });
            entity.HasIndex(e => new { e.ReferenciaTipo, e.ReferenciaId });
            entity.HasIndex(e => e.TemporadaId);
        });

        // Anulado = IsActive false (borrado lógico)
        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.PagoProveedor>(entity =>
        {
            entity.ToTable("PagoProveedor");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Medio).HasConversion<int>();
            entity.Property(e => e.Comprobante).HasMaxLength(100);
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Proveedor)
                .WithMany()
                .HasForeignKey(e => e.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CodigoCliente)
                .WithMany()
                .HasForeignKey(e => e.CodigoClienteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.TemporadaId);
            entity.HasIndex(e => e.ProveedorId);
            entity.HasIndex(e => e.CodigoClienteId);
        });

        // ===== Abastecimiento — Ajuste 2 =====
        // Stock por producto × almacén (reemplaza a LoteAlmacen)
        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.StockAlmacen>(entity =>
        {
            entity.ToTable("StockAlmacen", t => t.HasCheckConstraint("CK_StockAlmacen_CantidadNoNegativa", "[Cantidad] >= 0"));
            entity.HasKey(e => e.Id);
            // AlmacenId sin FK: Almacen vive en AuthDB
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ProductoId, e.AlmacenId }).IsUnique().HasDatabaseName("UX_StockAlmacen_Producto_Almacen");
            entity.HasIndex(e => e.TemporadaId);
            entity.HasIndex(e => e.AlmacenId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.Compra>(entity =>
        {
            entity.ToTable("Compra");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NroNota).HasMaxLength(50);
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Proveedor)
                .WithMany()
                .HasForeignKey(e => e.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PagoProveedor)
                .WithMany()
                .HasForeignKey(e => e.PagoProveedorId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.TemporadaId);
            entity.HasIndex(e => e.ProveedorId);
            entity.HasIndex(e => e.Fecha);
            entity.HasIndex(e => e.PagoProveedorId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.CompraDetalle>(entity =>
        {
            entity.ToTable("CompraDetalle");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrecioCompraUnidad).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Compra)
                .WithMany(c => c.Detalles)
                .HasForeignKey(e => e.CompraId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.CompraId, e.ProductoId }).IsUnique();
            entity.HasIndex(e => e.ProductoId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.CompraDistribucion>(entity =>
        {
            entity.ToTable("CompraDistribucion");
            entity.HasKey(e => e.Id);
            // AlmacenId sin FK: Almacen vive en AuthDB
            entity.HasOne(e => e.CompraDetalle)
                .WithMany(d => d.Distribuciones)
                .HasForeignKey(e => e.CompraDetalleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.CompraDetalleId, e.AlmacenId }).IsUnique();
        });
    }
}
