using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Servicio de stock reutilizable (recepciones, compras, ventas, traslados, ruta, conteos).
// Ajuste 2: stock por producto × almacén (StockAlmacen), sin lotes ni FIFO; costo promedio ponderado.
// Nunca abre transacción propia: los métodos que escriben corren dentro de la del llamador.
public class StockNavService : IStockNavService
{
    private readonly IStockAlmacenNavRepository _stockRepo;
    private readonly IMovimientoNavRepository _movimientoRepo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IRecepcionNavRepository _recepcionRepo;
    private readonly ICompraNavRepository _compraRepo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<StockNavService> _logger;

    public StockNavService(
        IStockAlmacenNavRepository stockRepo,
        IMovimientoNavRepository movimientoRepo,
        IProductoNavRepository productoRepo,
        IRecepcionNavRepository recepcionRepo,
        ICompraNavRepository compraRepo,
        IAlmacenRepository almacenRepo,
        ITemporadaActualService temporadaActual,
        ICurrentUserService currentUser,
        ILogger<StockNavService> logger)
    {
        _stockRepo = stockRepo;
        _movimientoRepo = movimientoRepo;
        _productoRepo = productoRepo;
        _recepcionRepo = recepcionRepo;
        _compraRepo = compraRepo;
        _almacenRepo = almacenRepo;
        _temporadaActual = temporadaActual;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task RegistrarEntradaAsync(int temporadaId, int almacenId, int productoId, int unidades,
        TipoMovimientoNav tipo, string referenciaTipo, int referenciaId, string? motivo = null)
    {
        ValidarUnidades(unidades);

        var stock = await _stockRepo.ObtenerAsync(productoId, almacenId);
        if (stock == null)
        {
            await _stockRepo.CrearAsync(new StockAlmacen
            {
                TemporadaId = temporadaId,
                ProductoId = productoId,
                AlmacenId = almacenId,
                Cantidad = unidades
            });
        }
        else
        {
            stock.Cantidad += unidades;
            await _stockRepo.ActualizarAsync(stock);
        }

        await RegistrarMovimientoAsync(temporadaId, almacenId, productoId, unidades, tipo, referenciaTipo, referenciaId, motivo);
    }

    /// <summary>
    /// Salida reutilizable: la usarán ventas, traslados, ruta y las anulaciones.
    /// Requiere la transacción del llamador (INavidadUnitOfWork); no abre una propia.
    /// </summary>
    public async Task RegistrarSalidaAsync(int temporadaId, int almacenId, int productoId, int unidades,
        TipoMovimientoNav tipo, string referenciaTipo, int referenciaId, string? motivo = null)
    {
        ValidarUnidades(unidades);

        var stock = await _stockRepo.ObtenerAsync(productoId, almacenId);
        var disponible = stock?.Cantidad ?? 0;
        if (stock == null || disponible < unidades)
        {
            var producto = await NombreProductoAsync(productoId);
            var almacen = await NombreAlmacenAsync(almacenId);
            throw new ExcepcionDominio(
                $"Stock insuficiente de '{producto}' en '{almacen}': disponible {disponible}, solicitado {unidades}",
                409, "STOCK_INSUFICIENTE");
        }

        stock.Cantidad -= unidades;
        await _stockRepo.ActualizarAsync(stock);

        await RegistrarMovimientoAsync(temporadaId, almacenId, productoId, -unidades, tipo, referenciaTipo, referenciaId, motivo);
    }

    public async Task<int> ObtenerDisponibleAsync(int almacenId, int productoId)
        => await _stockRepo.AsQueryable()
            .AsNoTracking()
            .Where(s => s.AlmacenId == almacenId && s.ProductoId == productoId)
            .Select(s => (int?)s.Cantidad)
            .FirstOrDefaultAsync() ?? 0;

    public async Task<Dictionary<int, decimal>> ObtenerCostosPromedioAsync(int temporadaId, IEnumerable<int>? productoIds = null)
    {
        var ids = productoIds?.Distinct().ToList();

        var recepciones = _recepcionRepo.AsQueryable().AsNoTracking()
            .Where(r => r.IsActive && !r.Anulada && r.TemporadaId == temporadaId)
            .SelectMany(r => r.Detalles)
            .Select(d => new { d.ProductoId, d.CantidadUnidades, d.PrecioCompraUnidad });
        var compras = _compraRepo.AsQueryable().AsNoTracking()
            .Where(c => c.IsActive && !c.Anulada && c.TemporadaId == temporadaId)
            .SelectMany(c => c.Detalles)
            .Select(d => new { d.ProductoId, d.CantidadUnidades, d.PrecioCompraUnidad });

        if (ids != null)
        {
            recepciones = recepciones.Where(d => ids.Contains(d.ProductoId));
            compras = compras.Where(d => ids.Contains(d.ProductoId));
        }

        var porRecepcion = await recepciones
            .GroupBy(d => d.ProductoId)
            .Select(g => new { ProductoId = g.Key, Unidades = g.Sum(x => x.CantidadUnidades), Monto = g.Sum(x => x.CantidadUnidades * x.PrecioCompraUnidad) })
            .ToListAsync();
        var porCompra = await compras
            .GroupBy(d => d.ProductoId)
            .Select(g => new { ProductoId = g.Key, Unidades = g.Sum(x => x.CantidadUnidades), Monto = g.Sum(x => x.CantidadUnidades * x.PrecioCompraUnidad) })
            .ToListAsync();

        return porRecepcion.Concat(porCompra)
            .GroupBy(x => x.ProductoId)
            .Select(g => new { ProductoId = g.Key, Unidades = g.Sum(x => x.Unidades), Monto = g.Sum(x => x.Monto) })
            .Where(x => x.Unidades > 0)
            .ToDictionary(x => x.ProductoId, x => x.Monto / x.Unidades);
    }

    public async Task<List<StockNavDto>> ObtenerStockAsync(StockNavFiltroDto filtro)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(filtro.TemporadaId);

            var query = _stockRepo.AsQueryable()
                .AsNoTracking()
                .Where(s => s.TemporadaId == temporadaId);

            if (filtro.AlmacenId.HasValue)
                query = query.Where(s => s.AlmacenId == filtro.AlmacenId.Value);
            if (filtro.ProductoId.HasValue)
                query = query.Where(s => s.ProductoId == filtro.ProductoId.Value);
            if (filtro.ProveedorId.HasValue)
                query = query.Where(s => s.Producto!.ProveedorId == filtro.ProveedorId.Value);
            if (!filtro.IncluirSinStock)
                query = query.Where(s => s.Cantidad > 0);

            var filas = await query
                .Select(s => new
                {
                    s.ProductoId,
                    s.AlmacenId,
                    s.Cantidad,
                    ProductoNombreMostrar = s.Producto!.Nombre ?? s.Producto.Descripcion,
                    s.Producto.ProveedorId,
                    ProveedorNombre = s.Producto.Proveedor != null ? s.Producto.Proveedor.Nombre : string.Empty,
                    s.Producto.UnidadesPorEmpaque,
                    s.Producto.NombreEmpaque
                })
                .ToListAsync();

            if (filas.Count == 0) return new List<StockNavDto>();

            var nombresAlmacen = await ObtenerNombresAlmacenAsync(filas.Select(f => f.AlmacenId).Distinct().ToList());
            var costos = await ObtenerCostosPromedioAsync(temporadaId, filas.Select(f => f.ProductoId));

            return filas
                .Select(f =>
                {
                    var costo = costos.GetValueOrDefault(f.ProductoId);
                    return new StockNavDto
                    {
                        ProductoId = f.ProductoId,
                        ProductoNombreMostrar = f.ProductoNombreMostrar,
                        ProveedorId = f.ProveedorId,
                        ProveedorNombre = f.ProveedorNombre,
                        UnidadesPorEmpaque = f.UnidadesPorEmpaque,
                        NombreEmpaque = f.NombreEmpaque,
                        AlmacenId = f.AlmacenId,
                        AlmacenNombre = nombresAlmacen.TryGetValue(f.AlmacenId, out var n) ? n : string.Empty,
                        Stock = f.Cantidad,
                        CostoPromedio = Redondear(costo),
                        Valorizado = Redondear(f.Cantidad * costo)
                    };
                })
                .OrderBy(s => s.AlmacenNombre)
                .ThenBy(s => s.AlmacenId)
                .ThenBy(s => s.ProductoNombreMostrar)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener stock navideño");
            throw;
        }
    }

    private async Task RegistrarMovimientoAsync(int temporadaId, int almacenId, int productoId, int cantidad,
        TipoMovimientoNav tipo, string referenciaTipo, int referenciaId, string? motivo)
    {
        await _movimientoRepo.CrearAsync(new MovimientoNav
        {
            TemporadaId = temporadaId,
            AlmacenId = almacenId,
            ProductoId = productoId,
            Cantidad = cantidad,
            Tipo = tipo,
            ReferenciaTipo = referenciaTipo,
            ReferenciaId = referenciaId,
            UsuarioId = _currentUser.GetUsuarioId() ?? 1,
            Fecha = DateTime.UtcNow,
            Motivo = motivo
        });
    }

    private async Task<string> NombreProductoAsync(int productoId)
        => await _productoRepo.AsQueryable().AsNoTracking()
            .Where(p => p.Id == productoId)
            .Select(p => p.Nombre ?? p.Descripcion)
            .FirstOrDefaultAsync() ?? $"#{productoId}";

    private async Task<string> NombreAlmacenAsync(int almacenId)
    {
        var nombres = await ObtenerNombresAlmacenAsync(new List<int> { almacenId });
        return nombres.TryGetValue(almacenId, out var n) ? n : $"#{almacenId}";
    }

    private async Task<Dictionary<int, string>> ObtenerNombresAlmacenAsync(List<int> almacenIds)
    {
        if (almacenIds.Count == 0) return new Dictionary<int, string>();
        return await _almacenRepo.AsQueryable()
            .AsNoTracking()
            .Where(a => almacenIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Nombre);
    }

    private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    private static void ValidarUnidades(int unidades)
    {
        if (unidades <= 0)
            throw new ValidacionException("La cantidad de unidades debe ser mayor a 0");
    }
}
