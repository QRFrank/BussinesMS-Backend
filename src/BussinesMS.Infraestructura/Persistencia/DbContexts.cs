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
    public DbSet<PermisoAuditoria> PermisoAuditorias => Set<PermisoAuditoria>();

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
            entity.Property(e => e.VerPreciosCompra).HasDefaultValue(false);
            entity.Property(e => e.PuedeHacerRecepciones).HasDefaultValue(false);

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

        modelBuilder.Entity<PermisoAuditoria>(entity =>
        {
            entity.ToTable("PermisoAuditoria");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Fecha).IsRequired();
            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.Metodo).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Ruta).IsRequired().HasMaxLength(400);
            entity.Property(e => e.Endpoint).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Menus).HasMaxLength(100);
            entity.Property(e => e.FlagRequerido).HasMaxLength(20);
            entity.Property(e => e.Motivo).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Modo).IsRequired().HasMaxLength(10);

            entity.HasIndex(e => new { e.UsuarioId, e.Endpoint });
            entity.HasIndex(e => e.Fecha);
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
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.CategoriaGastoTemporadaNav> CategoriasGastoTemporada => Set<BussinesMS.Dominio.Entidades.Navidad.CategoriaGastoTemporadaNav>();
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
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.Traslado> Traslados => Set<BussinesMS.Dominio.Entidades.Navidad.Traslado>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.TrasladoDetalle> TrasladosDetalle => Set<BussinesMS.Dominio.Entidades.Navidad.TrasladoDetalle>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.ConteoInventario> ConteosInventario => Set<BussinesMS.Dominio.Entidades.Navidad.ConteoInventario>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.ConteoInventarioDetalle> ConteosInventarioDetalle => Set<BussinesMS.Dominio.Entidades.Navidad.ConteoInventarioDetalle>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.VentaNav> VentasNav => Set<BussinesMS.Dominio.Entidades.Navidad.VentaNav>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.VentaNavDetalle> VentasNavDetalle => Set<BussinesMS.Dominio.Entidades.Navidad.VentaNavDetalle>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.EntregaVenta> EntregasVenta => Set<BussinesMS.Dominio.Entidades.Navidad.EntregaVenta>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.EntregaVentaDetalle> EntregasVentaDetalle => Set<BussinesMS.Dominio.Entidades.Navidad.EntregaVentaDetalle>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.ReservaBloqueo> ReservasBloqueo => Set<BussinesMS.Dominio.Entidades.Navidad.ReservaBloqueo>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.CobroVenta> CobrosVenta => Set<BussinesMS.Dominio.Entidades.Navidad.CobroVenta>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.ConsumoNav> ConsumosNav => Set<BussinesMS.Dominio.Entidades.Navidad.ConsumoNav>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.ConsumoNavDetalle> ConsumosNavDetalle => Set<BussinesMS.Dominio.Entidades.Navidad.ConsumoNavDetalle>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.SalidaRuta> SalidasRuta => Set<BussinesMS.Dominio.Entidades.Navidad.SalidaRuta>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.SalidaRutaCarga> SalidasRutaCarga => Set<BussinesMS.Dominio.Entidades.Navidad.SalidaRutaCarga>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.SalidaRutaCargaDetalle> SalidasRutaCargaDetalle => Set<BussinesMS.Dominio.Entidades.Navidad.SalidaRutaCargaDetalle>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.DevolucionRuta> DevolucionesRuta => Set<BussinesMS.Dominio.Entidades.Navidad.DevolucionRuta>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.DevolucionRutaDetalle> DevolucionesRutaDetalle => Set<BussinesMS.Dominio.Entidades.Navidad.DevolucionRutaDetalle>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.AbonoVendedor> AbonosVendedor => Set<BussinesMS.Dominio.Entidades.Navidad.AbonoVendedor>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.VentaClienteRuta> VentasClienteRuta => Set<BussinesMS.Dominio.Entidades.Navidad.VentaClienteRuta>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.VentaClienteRutaDetalle> VentasClienteRutaDetalle => Set<BussinesMS.Dominio.Entidades.Navidad.VentaClienteRutaDetalle>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.CobroClienteRuta> CobrosClienteRuta => Set<BussinesMS.Dominio.Entidades.Navidad.CobroClienteRuta>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.AbonoVendedorAplicacion> AbonosVendedorAplicacion => Set<BussinesMS.Dominio.Entidades.Navidad.AbonoVendedorAplicacion>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.PagoVendedor> PagosVendedor => Set<BussinesMS.Dominio.Entidades.Navidad.PagoVendedor>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.PagoVendedorDetalle> PagosVendedorDetalle => Set<BussinesMS.Dominio.Entidades.Navidad.PagoVendedorDetalle>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.CierreCajaTienda> CierresCajaTienda => Set<BussinesMS.Dominio.Entidades.Navidad.CierreCajaTienda>();
    public DbSet<BussinesMS.Dominio.Entidades.Navidad.TemporadaAlmacenEncargado> TemporadaAlmacenEncargados => Set<BussinesMS.Dominio.Entidades.Navidad.TemporadaAlmacenEncargado>();

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

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.TemporadaAlmacenEncargado>(entity =>
        {
            entity.ToTable("TemporadaAlmacenEncargado");
            entity.HasKey(e => e.Id);
            // AlmacenId y UsuarioId sin FK: viven en AuthDB
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.TemporadaId, e.AlmacenId, e.UsuarioId })
                .IsUnique()
                .HasDatabaseName("UX_TemporadaAlmacenEncargado");
            entity.HasIndex(e => new { e.TemporadaId, e.UsuarioId });
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

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.CategoriaGastoTemporadaNav>(entity =>
        {
            entity.ToTable("CategoriaGastoTemporadaNav");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.CategoriaGasto)
                .WithMany()
                .HasForeignKey(e => e.CategoriaGastoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.CategoriaGastoId, e.TemporadaId }).IsUnique();
            entity.HasIndex(e => e.TemporadaId);
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
            entity.Property(e => e.Color).HasMaxLength(20);
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
            entity.Property(e => e.FormaPago).HasConversion<int>();
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
            entity.HasIndex(e => e.TemporadaId);
            entity.HasIndex(e => e.ProveedorId);
            entity.HasIndex(e => e.Fecha);
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

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.Traslado>(entity =>
        {
            entity.ToTable("Traslado");
            entity.HasKey(e => e.Id);
            // AlmacenOrigenId, AlmacenDestinoId y UsuarioId sin FK: viven en AuthDB
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.Motivo).HasMaxLength(500);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.TemporadaId);
            entity.HasIndex(e => e.AlmacenOrigenId);
            entity.HasIndex(e => e.AlmacenDestinoId);
            entity.HasIndex(e => e.Fecha);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.TrasladoDetalle>(entity =>
        {
            entity.ToTable("TrasladoDetalle");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Traslado)
                .WithMany(t => t.Detalles)
                .HasForeignKey(e => e.TrasladoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.TrasladoId, e.ProductoId }).IsUnique();
            entity.HasIndex(e => e.ProductoId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.ConteoInventario>(entity =>
        {
            entity.ToTable("ConteoInventario");
            entity.HasKey(e => e.Id);
            // AlmacenId y UsuarioId sin FK: viven en AuthDB
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.Tipo).HasConversion<int>();
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            // No único: turnos secuenciales (varias aperturas/cierres por día)
            entity.HasIndex(e => new { e.TemporadaId, e.AlmacenId, e.Fecha, e.Tipo })
                .HasDatabaseName("IX_ConteoInventario_Temporada_Almacen_Fecha_Tipo");
            entity.HasIndex(e => e.AlmacenId);
            entity.HasIndex(e => e.Fecha);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.ConteoInventarioDetalle>(entity =>
        {
            entity.ToTable("ConteoInventarioDetalle", t => t.HasCheckConstraint("CK_ConteoInventarioDetalle_ContadoNoNegativo", "[Contado] >= 0"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Conteo)
                .WithMany(c => c.Detalles)
                .HasForeignKey(e => e.ConteoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ConteoId, e.ProductoId }).IsUnique();
            entity.HasIndex(e => e.ProductoId);
            entity.Property(e => e.MotivoDiferencia).HasConversion<int?>();
        });

        // ===== Ventas (módulo 6) =====
        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.VentaNav>(entity =>
        {
            entity.ToTable("VentaNav", t =>
            {
                t.HasCheckConstraint("CK_VentaNav_MontosNoNegativos",
                    "[MontoTotal] >= 0 AND [Descuento] >= 0 AND [MontoNeto] >= 0 AND [MontoCobrado] >= 0 AND [MontoSaldo] >= 0");
                t.HasCheckConstraint("CK_VentaNav_CobradoMenorIgualNeto", "[MontoCobrado] <= [MontoNeto]");
            });
            entity.HasKey(e => e.Id);
            // AlmacenId y UsuarioId sin FK: viven en AuthDB
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.Canal).HasConversion<int>();
            entity.Property(e => e.Tipo).HasConversion<int>();
            entity.Property(e => e.CondicionPago).HasConversion<int>();
            entity.Property(e => e.EstadoPago).HasConversion<int>();
            entity.Property(e => e.EstadoEntrega).HasConversion<int>();
            entity.Property(e => e.MontoTotal).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Descuento).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MontoNeto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MontoCobrado).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MontoSaldo).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Vendedor)
                .WithMany()
                .HasForeignKey(e => e.VendedorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Cliente)
                .WithMany()
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.TemporadaId, e.NroVenta }).IsUnique().HasDatabaseName("UX_VentaNav_Temporada_NroVenta");
            entity.HasIndex(e => e.AlmacenId);
            entity.HasIndex(e => e.VendedorId);
            entity.HasIndex(e => e.ClienteId);
            entity.HasIndex(e => e.Fecha);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.VentaNavDetalle>(entity =>
        {
            entity.ToTable("VentaNavDetalle", t =>
            {
                t.HasCheckConstraint("CK_VentaNavDetalle_EntregadasEnRango", "[UnidadesEntregadas] >= 0 AND [UnidadesEntregadas] <= [Unidades]");
                t.HasCheckConstraint("CK_VentaNavDetalle_MontosNoNegativos", "[Unidades] >= 0 AND [PrecioUnitario] >= 0 AND [Subtotal] >= 0");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)");
            entity.Property(e => e.EstadoEntrega).HasConversion<int>();
            entity.HasOne(e => e.Venta)
                .WithMany(v => v.Detalles)
                .HasForeignKey(e => e.VentaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.VentaId, e.ProductoId }).IsUnique();
            entity.HasIndex(e => e.ProductoId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.EntregaVenta>(entity =>
        {
            entity.ToTable("EntregaVenta");
            entity.HasKey(e => e.Id);
            // AlmacenId y UsuarioId sin FK: viven en AuthDB
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.HasOne(e => e.Venta)
                .WithMany(v => v.Entregas)
                .HasForeignKey(e => e.VentaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.VentaId);
            entity.HasIndex(e => e.AlmacenId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.EntregaVentaDetalle>(entity =>
        {
            entity.ToTable("EntregaVentaDetalle", t => t.HasCheckConstraint("CK_EntregaVentaDetalle_UnidadesNoNegativas", "[Unidades] >= 0"));
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Entrega)
                .WithMany(x => x.Detalles)
                .HasForeignKey(e => e.EntregaId)
                .OnDelete(DeleteBehavior.Cascade);
            // Sin cascada desde la línea de venta (evita múltiples rutas de cascada); las líneas nunca se borran
            entity.HasOne(e => e.VentaDetalle)
                .WithMany()
                .HasForeignKey(e => e.VentaDetalleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.EntregaId);
            entity.HasIndex(e => e.VentaDetalleId);
            entity.HasIndex(e => e.ProductoId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.ReservaBloqueo>(entity =>
        {
            entity.ToTable("ReservaBloqueo", t => t.HasCheckConstraint("CK_ReservaBloqueo_UnidadesPositivas", "[Unidades] > 0"));
            entity.HasKey(e => e.Id);
            // AlmacenId sin FK: vive en AuthDB
            entity.HasOne(e => e.Venta)
                .WithMany(v => v.Bloqueos)
                .HasForeignKey(e => e.VentaId)
                .OnDelete(DeleteBehavior.Cascade);
            // Sin cascada desde la línea, el producto ni la recepción (evita múltiples rutas de cascada)
            entity.HasOne(e => e.VentaDetalle)
                .WithMany()
                .HasForeignKey(e => e.VentaDetalleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Recepcion)
                .WithMany()
                .HasForeignKey(e => e.RecepcionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.AlmacenId, e.ProductoId });
            entity.HasIndex(e => e.VentaDetalleId);
            entity.HasIndex(e => e.RecepcionId);
            entity.HasIndex(e => e.VentaId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.CobroVenta>(entity =>
        {
            entity.ToTable("CobroVenta", t => t.HasCheckConstraint("CK_CobroVenta_MontoPositivo", "[Monto] > 0"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Medio).HasConversion<int>();
            entity.Property(e => e.Comprobante).HasMaxLength(100);
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Venta)
                .WithMany(v => v.Cobros)
                .HasForeignKey(e => e.VentaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AbonoVendedor)
                .WithMany()
                .HasForeignKey(e => e.AbonoVendedorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.VentaId);
            entity.HasIndex(e => e.AbonoVendedorId);
            // AlmacenId sin FK: vive en AuthDB
            entity.HasIndex(e => e.AlmacenId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.CierreCajaTienda>(entity =>
        {
            entity.ToTable("CierreCajaTienda");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Desde).HasColumnType("datetime2");
            entity.Property(e => e.Hasta).HasColumnType("datetime2");
            entity.Property(e => e.TotalVendido).HasColumnType("decimal(18,2)");
            entity.Property(e => e.EfectivoEsperado).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Transferencias).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Credito).HasColumnType("decimal(18,2)");
            entity.Property(e => e.EfectivoEntregado).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Diferencia).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Temporada).WithMany().HasForeignKey(e => e.TemporadaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ConteoApertura).WithMany().HasForeignKey(e => e.ConteoAperturaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ConteoCierre).WithMany().HasForeignKey(e => e.ConteoCierreId).OnDelete(DeleteBehavior.Restrict);
            // Una entrega por cierre de conteo
            entity.HasIndex(e => e.ConteoCierreId).IsUnique();
            entity.HasIndex(e => new { e.TemporadaId, e.AlmacenId });
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.ConsumoNav>(entity =>
        {
            entity.ToTable("ConsumoNav");
            entity.HasKey(e => e.Id);
            // AlmacenId y UsuarioId sin FK: viven en AuthDB
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.Motivo).HasConversion<int>();
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.TemporadaId);
            entity.HasIndex(e => e.AlmacenId);
            entity.HasIndex(e => e.Fecha);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.ConsumoNavDetalle>(entity =>
        {
            entity.ToTable("ConsumoNavDetalle", t => t.HasCheckConstraint("CK_ConsumoNavDetalle_UnidadesPositivas", "[Unidades] > 0"));
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Consumo)
                .WithMany(c => c.Detalles)
                .HasForeignKey(e => e.ConsumoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ConsumoId, e.ProductoId }).IsUnique();
            entity.HasIndex(e => e.ProductoId);
        });

        // Registro personal de ventas del vendedor de ruta (privado, no afecta stock ni ventas del sistema)
        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.VentaClienteRuta>(entity =>
        {
            entity.ToTable("VentaClienteRuta");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.Total).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Pagado).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Saldo).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Temporada).WithMany().HasForeignKey(e => e.TemporadaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Vendedor).WithMany().HasForeignKey(e => e.VendedorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Cliente).WithMany().HasForeignKey(e => e.ClienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.SalidaRuta).WithMany().HasForeignKey(e => e.SalidaRutaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.TemporadaId, e.VendedorId, e.Fecha });
            entity.HasIndex(e => new { e.VendedorId, e.ClienteId });
            entity.HasIndex(e => e.SalidaRutaId);
            entity.HasIndex(e => e.ClienteId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.VentaClienteRutaDetalle>(entity =>
        {
            entity.ToTable("VentaClienteRutaDetalle", t =>
            {
                t.HasCheckConstraint("CK_VentaClienteRutaDetalle_UnidadesPositivas", "[Unidades] > 0");
                t.HasCheckConstraint("CK_VentaClienteRutaDetalle_PrecioNoNegativo", "[PrecioUnitario] >= 0");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.VentaClienteRuta).WithMany(v => v.Detalles).HasForeignKey(e => e.VentaClienteRutaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Producto).WithMany().HasForeignKey(e => e.ProductoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.VentaClienteRutaId, e.ProductoId }).IsUnique();
            entity.HasIndex(e => e.ProductoId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.CobroClienteRuta>(entity =>
        {
            entity.ToTable("CobroClienteRuta", t => t.HasCheckConstraint("CK_CobroClienteRuta_MontoPositivo", "[Monto] > 0"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Medio).HasConversion<int?>();
            entity.HasOne(e => e.VentaClienteRuta).WithMany(v => v.Cobros).HasForeignKey(e => e.VentaClienteRutaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.VentaClienteRutaId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.SalidaRuta>(entity =>
        {
            entity.ToTable("SalidaRuta");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FechaApertura).HasColumnType("datetime2");
            entity.Property(e => e.FechaCierre).HasColumnType("datetime2");
            entity.Property(e => e.Estado).HasConversion<int>();
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Vendedor)
                .WithMany()
                .HasForeignKey(e => e.VendedorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Venta)
                .WithMany()
                .HasForeignKey(e => e.VentaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.TemporadaId, e.VendedorId, e.Estado });
            // Una sola salida abierta por vendedor y temporada
            entity.HasIndex(e => new { e.TemporadaId, e.VendedorId })
                .IsUnique()
                .HasFilter("[IsActive] = 1 AND [Estado] = 1");
            entity.HasIndex(e => e.VendedorId);
            entity.HasIndex(e => e.VentaId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.SalidaRutaCarga>(entity =>
        {
            entity.ToTable("SalidaRutaCarga");
            entity.HasKey(e => e.Id);
            // UsuarioId sin FK: vive en AuthDB
            entity.Property(e => e.Fecha).HasColumnType("datetime2");
            entity.HasOne(e => e.SalidaRuta)
                .WithMany(s => s.Cargas)
                .HasForeignKey(e => e.SalidaRutaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.SalidaRutaId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.SalidaRutaCargaDetalle>(entity =>
        {
            entity.ToTable("SalidaRutaCargaDetalle", t => t.HasCheckConstraint("CK_SalidaRutaCargaDetalle_UnidadesPositivas", "[Unidades] > 0"));
            entity.HasKey(e => e.Id);
            // AlmacenOrigenId sin FK: vive en AuthDB
            entity.HasOne(e => e.Carga)
                .WithMany(c => c.Detalles)
                .HasForeignKey(e => e.CargaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.CargaId, e.AlmacenOrigenId, e.ProductoId }).IsUnique();
            entity.HasIndex(e => e.ProductoId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.DevolucionRuta>(entity =>
        {
            entity.ToTable("DevolucionRuta");
            entity.HasKey(e => e.Id);
            // UsuarioId sin FK: vive en AuthDB
            entity.Property(e => e.Fecha).HasColumnType("datetime2");
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.SalidaRuta)
                .WithMany(s => s.Devoluciones)
                .HasForeignKey(e => e.SalidaRutaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.SalidaRutaId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.DevolucionRutaDetalle>(entity =>
        {
            entity.ToTable("DevolucionRutaDetalle", t => t.HasCheckConstraint("CK_DevolucionRutaDetalle_UnidadesPositivas", "[Unidades] > 0"));
            entity.HasKey(e => e.Id);
            // AlmacenDestinoId sin FK: vive en AuthDB
            entity.HasOne(e => e.Devolucion)
                .WithMany(d => d.Detalles)
                .HasForeignKey(e => e.DevolucionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.DevolucionId, e.AlmacenDestinoId, e.ProductoId }).IsUnique();
            entity.HasIndex(e => e.ProductoId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.AbonoVendedor>(entity =>
        {
            entity.ToTable("AbonoVendedor", t => t.HasCheckConstraint("CK_AbonoVendedor_Montos", "[Monto] >= 0 AND [Descuento] >= 0 AND [Monto] + [Descuento] > 0"));
            entity.HasKey(e => e.Id);
            // UsuarioId sin FK: vive en AuthDB
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Descuento).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Medio).HasConversion<int?>();
            entity.Property(e => e.Comprobante).HasMaxLength(100);
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Vendedor)
                .WithMany()
                .HasForeignKey(e => e.VendedorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.TemporadaId, e.VendedorId });
            entity.HasIndex(e => e.VendedorId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.AbonoVendedorAplicacion>(entity =>
        {
            entity.ToTable("AbonoVendedorAplicacion", t => t.HasCheckConstraint("CK_AbonoVendedorAplicacion_Montos", "[MontoCobrado] >= 0 AND [MontoDescuento] >= 0 AND [MontoCobrado] + [MontoDescuento] > 0"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MontoCobrado).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MontoDescuento).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.AbonoVendedor)
                .WithMany(a => a.Aplicaciones)
                .HasForeignKey(e => e.AbonoVendedorId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Venta)
                .WithMany()
                .HasForeignKey(e => e.VentaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.AbonoVendedorId);
            entity.HasIndex(e => e.VentaId);
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.PagoVendedor>(entity =>
        {
            entity.ToTable("PagoVendedor", t => t.HasCheckConstraint("CK_PagoVendedor_Monto", "[Monto] > 0"));
            entity.HasKey(e => e.Id);
            // UsuarioId sin FK: vive en AuthDB
            entity.Property(e => e.Fecha).HasColumnType("date");
            entity.Property(e => e.Tipo).HasConversion<int>();
            entity.Property(e => e.Periodo).HasMaxLength(7);
            entity.Property(e => e.ModoComision).HasConversion<int?>();
            entity.Property(e => e.Porcentaje).HasColumnType("decimal(5,2)");
            entity.Property(e => e.MontoBase).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Medio).HasConversion<int>();
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasOne(e => e.Temporada)
                .WithMany()
                .HasForeignKey(e => e.TemporadaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Vendedor)
                .WithMany()
                .HasForeignKey(e => e.VendedorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.TemporadaId, e.VendedorId, e.Tipo });
            entity.HasIndex(e => e.VendedorId);
            // Un solo pago de comisión vigente por vendedor y temporada
            entity.HasIndex(e => new { e.TemporadaId, e.VendedorId })
                .IsUnique()
                .HasFilter("[Tipo] = 2 AND [Anulado] = 0 AND [IsActive] = 1")
                .HasDatabaseName("UX_PagoVendedor_ComisionVigente");
        });

        modelBuilder.Entity<BussinesMS.Dominio.Entidades.Navidad.PagoVendedorDetalle>(entity =>
        {
            entity.ToTable("PagoVendedorDetalle", t => t.HasCheckConstraint("CK_PagoVendedorDetalle_Montos", "[Unidades] > 0 AND [ComisionUnidad] >= 0 AND [Subtotal] >= 0"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ComisionUnidad).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.PagoVendedor)
                .WithMany(p => p.Detalles)
                .HasForeignKey(e => e.PagoVendedorId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Producto)
                .WithMany()
                .HasForeignKey(e => e.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.PagoVendedorId, e.ProductoId }).IsUnique();
            entity.HasIndex(e => e.ProductoId);
        });
    }
}
