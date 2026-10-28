using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Reportes del módulo 9 (solo admin, solo lectura, temporada abierta). Los totales que también están en el dashboard salen de
// ResumenNavCalculos (el mismo método), así que dan los mismos números; sin filtro de fechas cubren la temporada completa.
// Costo de lo vendido, consumo y ajustes: costo promedio ponderado por producto (IStockNavService.ObtenerCostosPromedioAsync).
public class ReportesNavService : IReportesNavService
{
    private readonly ResumenNavCalculos _calc;
    private readonly IAbastecimientoNavService _abastecimiento;
    private readonly IRutaNavService _ruta;
    private readonly IPagoVendedorNavService _pagosVendedores;
    private readonly IAporteCapitalService _capital;
    private readonly IStockNavService _stock;
    private readonly IAlmacenAccesoNavService _acceso;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly IGastoNavRepository _gastoRepo;
    private readonly IStockAlmacenNavRepository _stockRepo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly VentaNavService _ventas;
    private readonly ILogger<ReportesNavService> _logger;

    public ReportesNavService(
        ResumenNavCalculos calc,
        IAbastecimientoNavService abastecimiento,
        IRutaNavService ruta,
        IPagoVendedorNavService pagosVendedores,
        IAporteCapitalService capital,
        IStockNavService stock,
        IAlmacenAccesoNavService acceso,
        ITemporadaActualService temporadaActual,
        IGastoNavRepository gastoRepo,
        IStockAlmacenNavRepository stockRepo,
        IProductoNavRepository productoRepo,
        IAlmacenRepository almacenRepo,
        VentaNavService ventas,
        ILogger<ReportesNavService> logger)
    {
        _calc = calc;
        _abastecimiento = abastecimiento;
        _ruta = ruta;
        _pagosVendedores = pagosVendedores;
        _capital = capital;
        _stock = stock;
        _acceso = acceso;
        _temporadaActual = temporadaActual;
        _gastoRepo = gastoRepo;
        _stockRepo = stockRepo;
        _productoRepo = productoRepo;
        _almacenRepo = almacenRepo;
        _ventas = ventas;
        _logger = logger;
    }

    // ================= Comunes =================

    private async Task<BussinesMS.Dominio.Entidades.Navidad.Temporada> ExigirAdminYTemporadaAsync()
    {
        if (!await _acceso.EsAdminAsync())
            throw new ExcepcionDominio("Solo el admin puede ver los reportes", 403, "SIN_PERMISO");
        return await _temporadaActual.ObtenerAbiertaAsync();
    }

    private static DashboardTemporadaNavDto MapearTemporada(BussinesMS.Dominio.Entidades.Navidad.Temporada t) => new()
    {
        Id = t.Id,
        Nombre = t.Nombre,
        Anio = t.Anio,
        FechaInicio = t.FechaInicio
    };

    private static void ValidarFechas(DateTime? desde, DateTime? hasta)
    {
        if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
            throw new ValidacionException("La fecha inicial no puede ser mayor a la final");
    }

    private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    private static IQueryable<T> FiltrarFecha<T>(IQueryable<T> query, DateTime? desde, DateTime? hasta,
        System.Linq.Expressions.Expression<Func<T, DateTime>> fecha)
    {
        if (desde.HasValue)
        {
            var d = desde.Value.Date;
            query = query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                System.Linq.Expressions.Expression.GreaterThanOrEqual(fecha.Body, System.Linq.Expressions.Expression.Constant(d)), fecha.Parameters));
        }
        if (hasta.HasValue)
        {
            var h = hasta.Value.Date.AddDays(1);
            query = query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                System.Linq.Expressions.Expression.LessThan(fecha.Body, System.Linq.Expressions.Expression.Constant(h)), fecha.Parameters));
        }
        return query;
    }

    private sealed class ProductoInfo
    {
        public int Id { get; set; }
        public string NombreMostrar { get; set; } = string.Empty;
        public int ProveedorId { get; set; }
        public string ProveedorNombre { get; set; } = string.Empty;
        public int? UnidadesPorEmpaque { get; set; }
        public string? NombreEmpaque { get; set; }
        public string? Color { get; set; }
    }

    private async Task<Dictionary<int, ProductoInfo>> ObtenerProductosAsync(IEnumerable<int> productoIds)
    {
        var ids = productoIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, ProductoInfo>();
        var lista = await _productoRepo.AsQueryable().AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new ProductoInfo
            {
                Id = p.Id,
                NombreMostrar = p.Nombre ?? p.Descripcion,
                ProveedorId = p.ProveedorId,
                ProveedorNombre = p.Proveedor != null ? p.Proveedor.Nombre : string.Empty,
                UnidadesPorEmpaque = p.UnidadesPorEmpaque,
                NombreEmpaque = p.NombreEmpaque,
                Color = p.Color
            })
            .ToListAsync();
        return lista.ToDictionary(p => p.Id);
    }

    // ================= Resultado =================

    public async Task<ReporteResultadoNavDto> ObtenerResultadoAsync(DateTime? desde, DateTime? hasta)
    {
        try
        {
            var temporada = await ExigirAdminYTemporadaAsync();
            ValidarFechas(desde, hasta);
            var t = temporada.Id;

            return new ReporteResultadoNavDto
            {
                Temporada = MapearTemporada(temporada),
                Desde = desde?.Date,
                Hasta = hasta?.Date,
                Resultado = await _calc.ObtenerResultadoAsync(t, desde, hasta),
                Capital = await _calc.ObtenerCapitalAsync(t),
                FlujoEfectivo = await _calc.ObtenerFlujoEfectivoAsync(t, desde, hasta),
                NosDeben = await _calc.ObtenerNosDebenAsync(t),
                Debo = await _calc.ObtenerDeboAsync(t),
                Gastos = await _calc.ObtenerGastosTarjetaAsync(t, desde, hasta),
                Perdidas = await _calc.ObtenerPerdidasAsync(t, desde, hasta),
                ReservasPendientes = await _calc.ObtenerReservasPendientesAsync(t),
                Conciliacion = await _calc.ObtenerConciliacionAsync(t)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el reporte de resultado");
            throw;
        }
    }

    // ================= Ventas =================

    public async Task<ReporteVentasNavDto> ObtenerVentasAsync(ReporteVentasNavFiltrosDto filtros)
    {
        try
        {
            var temporada = await ExigirAdminYTemporadaAsync();
            ValidarFechas(filtros.Desde, filtros.Hasta);
            if (filtros.Canal.HasValue && !Enum.IsDefined(typeof(CanalVentaNav), filtros.Canal.Value))
                throw new ValidacionException("El canal es inválido (1 Tienda, 2 Ruta)");
            if (filtros.Tipo.HasValue && !Enum.IsDefined(typeof(TipoVentaNav), filtros.Tipo.Value))
                throw new ValidacionException("El tipo es inválido (1 Directa, 2 Reserva)");

            var t = temporada.Id;
            var filtro = new FiltroResumenVentas
            {
                Desde = filtros.Desde,
                Hasta = filtros.Hasta,
                AlmacenId = filtros.AlmacenId,
                VendedorId = filtros.VendedorId,
                Canal = (CanalVentaNav?)filtros.Canal,
                Tipo = (TipoVentaNav?)filtros.Tipo
            };

            var totales = await _calc.ObtenerVentasAsync(t, filtro);
            var reservasPendientes = await _calc.ObtenerReservasPendientesAsync(t, filtro);

            // Solo lo que cuenta como venta (las reservas, por lo entregado)
            var ventasFiltradas = (await _calc.CargarVentasAsync(t, filtro)).Where(v => v.EsVenta).ToList();

            var porDia = ventasFiltradas
                .GroupBy(v => v.Fecha.Date)
                .OrderBy(g => g.Key)
                .Select(g => new ReporteVentaDiaNavDto
                {
                    Fecha = DateTime.SpecifyKind(g.Key, DateTimeKind.Unspecified),
                    CantidadVentas = g.Count(),
                    ImporteTotal = g.Sum(x => x.Importe),
                    Descuentos = g.Sum(x => x.DescuentoVenta),
                    Neto = g.Sum(x => x.Neto),
                    Cobrado = g.Sum(x => x.Cobrado),
                    PorCobrar = g.Sum(x => x.Saldo)
                })
                .ToList();

            var almacenFilas = ventasFiltradas
                .GroupBy(v => v.AlmacenId)
                .Select(g => new
                {
                    AlmacenId = g.Key,
                    Cantidad = g.Count(),
                    Total = g.Sum(x => x.Importe),
                    Descuento = g.Sum(x => x.DescuentoVenta),
                    Neto = g.Sum(x => x.Neto),
                    Cobrado = g.Sum(x => x.Cobrado),
                    Saldo = g.Sum(x => x.Saldo)
                })
                .ToList();
            var nombresAlmacen = await _ventas.ObtenerNombresAlmacenAsync(almacenFilas.Where(a => a.AlmacenId.HasValue).Select(a => a.AlmacenId!.Value));
            var porAlmacen = almacenFilas
                .Select(a => new ReporteVentaAlmacenNavDto
                {
                    AlmacenId = a.AlmacenId,
                    AlmacenNombre = a.AlmacenId.HasValue ? nombresAlmacen.GetValueOrDefault(a.AlmacenId.Value, $"#{a.AlmacenId}") : "Sin almacén (ruta)",
                    CantidadVentas = a.Cantidad,
                    ImporteTotal = a.Total,
                    Descuentos = a.Descuento,
                    Neto = a.Neto,
                    Cobrado = a.Cobrado,
                    PorCobrar = a.Saldo
                })
                .OrderByDescending(a => a.Neto)
                .ToList();

            // Por vendedor: el reporte del módulo 8, igual que tablas.ventasPorVendedor del dashboard
            var reporteVendedores = await _pagosVendedores.ObtenerReporteAsync(new ReporteVentasVendedorFiltroDto
            {
                VendedorId = filtros.VendedorId,
                Canal = filtros.Canal,
                Desde = filtros.Desde,
                Hasta = filtros.Hasta,
                AlmacenId = filtros.AlmacenId,
                Tipo = filtros.Tipo
            });

            // Por producto: unidades y neto del mismo cálculo del dashboard; el costo es unidades × costo promedio
            var vendido = await _calc.ObtenerVendidoPorProductoAsync(t, filtro);
            var costos = await _stock.ObtenerCostosPromedioAsync(t);
            var productos = await ObtenerProductosAsync(vendido.Select(v => v.ProductoId));
            var porProducto = vendido
                .Select(v =>
                {
                    productos.TryGetValue(v.ProductoId, out var p);
                    var costoProm = costos.GetValueOrDefault(v.ProductoId);
                    var neto = Redondear(v.Neto);
                    var costo = Redondear(v.Unidades * costoProm);
                    return new ReporteVentaProductoNavDto
                    {
                        ProductoId = v.ProductoId,
                        ProductoNombreMostrar = p?.NombreMostrar ?? string.Empty,
                        UnidadesPorEmpaque = p?.UnidadesPorEmpaque,
                        NombreEmpaque = p?.NombreEmpaque,
                        Color = p?.Color,
                        Unidades = v.Unidades,
                        Neto = neto,
                        CostoPromedio = Redondear(costoProm),
                        Costo = costo,
                        Ganancia = neto - costo
                    };
                })
                .OrderByDescending(x => x.Neto)
                .ToList();

            return new ReporteVentasNavDto
            {
                Temporada = MapearTemporada(temporada),
                Filtros = new ReporteVentasNavFiltrosDto
                {
                    Desde = filtros.Desde?.Date,
                    Hasta = filtros.Hasta?.Date,
                    AlmacenId = filtros.AlmacenId,
                    VendedorId = filtros.VendedorId,
                    Canal = filtros.Canal,
                    Tipo = filtros.Tipo
                },
                Totales = totales,
                ReservasPendientes = reservasPendientes,
                PorDia = porDia,
                PorVendedor = _calc.ConstruirVentasPorVendedor(reporteVendedores),
                PorAlmacen = porAlmacen,
                PorProducto = porProducto,
                TotalesProductos = new ReporteVentasProductosTotalesNavDto
                {
                    Unidades = porProducto.Sum(x => x.Unidades),
                    Neto = porProducto.Sum(x => x.Neto),
                    Costo = porProducto.Sum(x => x.Costo),
                    Ganancia = porProducto.Sum(x => x.Ganancia)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el reporte de ventas");
            throw;
        }
    }

    // ================= Stock =================

    public async Task<ReporteStockNavDto> ObtenerStockAsync(int? almacenId, int? proveedorId, bool incluirSinStock)
    {
        try
        {
            var temporada = await ExigirAdminYTemporadaAsync();
            var t = temporada.Id;

            var query = _stockRepo.AsQueryable().AsNoTracking().Where(s => s.IsActive && s.TemporadaId == t);
            if (almacenId.HasValue) query = query.Where(s => s.AlmacenId == almacenId.Value);
            if (proveedorId.HasValue) query = query.Where(s => s.Producto!.ProveedorId == proveedorId.Value);

            var filas = await query.Select(s => new { s.ProductoId, s.AlmacenId, s.Cantidad }).ToListAsync();
            var costos = await _stock.ObtenerCostosPromedioAsync(t, filas.Select(f => f.ProductoId));

            var almacenIds = filas.Select(f => f.AlmacenId).Distinct().ToList();
            var almacenes = await _almacenRepo.AsQueryable().AsNoTracking()
                .Where(a => almacenIds.Contains(a.Id))
                .Select(a => new { a.Id, a.Nombre, a.EsTienda })
                .ToListAsync();
            var columnas = almacenes.OrderBy(a => a.Nombre, StringComparer.CurrentCultureIgnoreCase).ThenBy(a => a.Id).ToList();

            var porProducto = filas.GroupBy(f => f.ProductoId).ToDictionary(g => g.Key, g => g.ToList());
            var productos = await ObtenerProductosAsync(porProducto.Keys);

            var filasProducto = porProducto
                .Select(kv =>
                {
                    productos.TryGetValue(kv.Key, out var p);
                    var costo = costos.GetValueOrDefault(kv.Key);
                    var total = kv.Value.Sum(x => x.Cantidad);
                    return new ReporteStockProductoNavDto
                    {
                        ProductoId = kv.Key,
                        ProductoNombreMostrar = p?.NombreMostrar ?? string.Empty,
                        ProveedorId = p?.ProveedorId ?? 0,
                        ProveedorNombre = p?.ProveedorNombre ?? string.Empty,
                        UnidadesPorEmpaque = p?.UnidadesPorEmpaque,
                        NombreEmpaque = p?.NombreEmpaque,
                        Color = p?.Color,
                        Unidades = columnas.Select(a => kv.Value.Where(x => x.AlmacenId == a.Id).Sum(x => x.Cantidad)).ToList(),
                        Total = total,
                        CostoPromedio = Redondear(costo),
                        Valorizado = Redondear(total * costo)
                    };
                })
                .Where(p => incluirSinStock || p.Total > 0)
                .OrderBy(p => p.ProductoNombreMostrar, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            var idsMostrados = filasProducto.Select(p => p.ProductoId).ToHashSet();
            var mostradas = filas.Where(f => idsMostrados.Contains(f.ProductoId)).ToList();

            return new ReporteStockNavDto
            {
                Temporada = MapearTemporada(temporada),
                Almacenes = columnas
                    .Select(a => new ReporteStockAlmacenNavDto
                    {
                        AlmacenId = a.Id,
                        AlmacenNombre = a.Nombre,
                        EsTienda = a.EsTienda,
                        Unidades = mostradas.Where(f => f.AlmacenId == a.Id).Sum(f => f.Cantidad),
                        Valorizado = Redondear(mostradas.Where(f => f.AlmacenId == a.Id).Sum(f => f.Cantidad * costos.GetValueOrDefault(f.ProductoId)))
                    })
                    .ToList(),
                Productos = filasProducto,
                TotalUnidades = filasProducto.Sum(p => p.Total),
                TotalValorizado = await _calc.ObtenerStockValorizadoAsync(t, almacenId, proveedorId)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el reporte de stock");
            throw;
        }
    }

    // ================= Faltantes =================

    public async Task<ReporteFaltantesNavDto> ObtenerFaltantesAsync(int? proveedorId, int? codigoClienteId)
    {
        try
        {
            var temporada = await ExigirAdminYTemporadaAsync();
            var faltantes = await _abastecimiento.ObtenerFaltantesAsync(new AbastecimientoNavFiltroDto
            {
                ProveedorId = proveedorId,
                CodigoClienteId = codigoClienteId
            });

            var bloque = _calc.ConstruirFaltaLlegar(faltantes);
            var porCodigo = bloque.Items
                .GroupBy(i => new { i.ProveedorId, i.CodigoClienteId })
                .Select(g => new ReporteFaltanteCodigoNavDto
                {
                    ProveedorId = g.Key.ProveedorId,
                    ProveedorNombre = g.First().ProveedorNombre,
                    CodigoClienteId = g.Key.CodigoClienteId,
                    Codigo = g.First().Codigo,
                    CodigoTitular = faltantes.FirstOrDefault(f => f.ProveedorId == g.Key.ProveedorId && f.CodigoClienteId == g.Key.CodigoClienteId)?.CodigoTitular,
                    Productos = g.Count(),
                    Pedido = g.Sum(x => x.Pedido),
                    Recibido = g.Sum(x => x.Recibido),
                    Faltante = g.Sum(x => x.Faltante)
                })
                .OrderByDescending(x => x.Faltante)
                .ToList();

            return new ReporteFaltantesNavDto
            {
                Temporada = MapearTemporada(temporada),
                TotalUnidades = bloque.TotalUnidades,
                PorCodigo = porCodigo,
                Items = bloque.Items
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el reporte de faltantes");
            throw;
        }
    }

    // ================= Deudas =================

    public async Task<ReporteDeudasNavDto> ObtenerDeudasAsync()
    {
        try
        {
            var temporada = await ExigirAdminYTemporadaAsync();
            var t = temporada.Id;

            var deudasProveedor = await _abastecimiento.ObtenerDeudasAsync(new AbastecimientoNavFiltroDto());
            var deudasRuta = await _ruta.ObtenerDeudasAsync(null);
            var sueldos = await _pagosVendedores.ObtenerResumenSueldosAsync(null, null);
            var reporte = await _pagosVendedores.ObtenerReporteAsync(new ReporteVentasVendedorFiltroDto());
            var capital = await _capital.ObtenerResumenAsync(null);

            var ventasConSaldo = await _calc.ObtenerVentasTiendaConSaldoAsync(t);

            var detalle = ventasConSaldo
                .GroupBy(v => v.ClienteId)
                .Select(g => new ReporteDeudaClienteNavDto
                {
                    ClienteId = g.Key,
                    ClienteNombre = g.Select(x => x.ClienteNombre).FirstOrDefault(n => n != null) ?? "Sin cliente",
                    SaldoTotal = g.Sum(x => x.Saldo),
                    Ventas = g.OrderBy(x => x.Fecha).ThenBy(x => x.Id)
                        .Select(x => new ReporteDeudaVentaNavDto
                        {
                            VentaId = x.Id,
                            NroVenta = x.NroVenta,
                            Fecha = x.Fecha,
                            Tipo = x.Tipo == TipoVentaNav.Reserva ? "Reserva" : "Credito",
                            Neto = x.Neto,
                            Cobrado = x.Cobrado,
                            Saldo = x.Saldo
                        })
                        .ToList()
                })
                .OrderByDescending(c => c.SaldoTotal)
                .ToList();

            return new ReporteDeudasNavDto
            {
                Temporada = MapearTemporada(temporada),
                Proveedores = _calc.ConstruirDeudaProveedores(deudasProveedor),
                Clientes = await _calc.ObtenerDeudaClientesAsync(t),
                ClientesDetalle = detalle,
                VendedoresRuta = _calc.ConstruirDeudaRuta(deudasRuta),
                Inversores = _calc.ConstruirDeudaInversores(capital),
                InversoresAportes = capital.Aportes,
                Sueldos = _calc.ConstruirSueldos(sueldos),
                Comisiones = await _calc.ConstruirComisionesAsync(t, reporte)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el reporte de deudas");
            throw;
        }
    }

    // ================= Gastos =================

    public async Task<ReporteGastosNavDto> ObtenerGastosAsync(DateTime? desde, DateTime? hasta, int? categoriaId)
    {
        try
        {
            var temporada = await ExigirAdminYTemporadaAsync();
            ValidarFechas(desde, hasta);
            var t = temporada.Id;

            var bloque = await _calc.ObtenerGastosAsync(t, null, desde, hasta, categoriaId);

            var query = FiltrarFecha(
                _gastoRepo.AsQueryable().AsNoTracking().Where(g => g.IsActive && g.TemporadaId == t),
                desde, hasta, g => g.Fecha);
            if (categoriaId.HasValue) query = query.Where(g => g.CategoriaId == categoriaId.Value);

            var porCategoria = await query
                .GroupBy(g => new { g.CategoriaId, Nombre = g.Categoria != null ? g.Categoria.Nombre : string.Empty })
                .Select(g => new ReporteGastoCategoriaNavDto
                {
                    CategoriaId = g.Key.CategoriaId,
                    CategoriaNombre = g.Key.Nombre,
                    Cantidad = g.Count(),
                    Total = g.Sum(x => x.Monto)
                })
                .ToListAsync();

            return new ReporteGastosNavDto
            {
                Temporada = MapearTemporada(temporada),
                Desde = desde?.Date,
                Hasta = hasta?.Date,
                CategoriaId = categoriaId,
                Total = bloque.Total,
                Cantidad = bloque.Cantidad,
                PorCategoria = porCategoria.OrderByDescending(c => c.Total).ToList(),
                Items = bloque.Items
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el reporte de gastos");
            throw;
        }
    }

    // ================= Pagos a proveedores =================

    public async Task<ReportePagosProveedoresNavDto> ObtenerPagosProveedoresAsync(DateTime? desde, DateTime? hasta, int? proveedorId, int? codigoClienteId)
    {
        try
        {
            var temporada = await ExigirAdminYTemporadaAsync();
            ValidarFechas(desde, hasta);

            var deudas = await _abastecimiento.ObtenerDeudasAsync(new AbastecimientoNavFiltroDto
            {
                ProveedorId = proveedorId,
                CodigoClienteId = codigoClienteId
            });
            var bloque = await _calc.ConstruirPagosProveedoresAsync(temporada.Id, deudas, null, desde, hasta, proveedorId, codigoClienteId);

            return new ReportePagosProveedoresNavDto
            {
                Temporada = MapearTemporada(temporada),
                Desde = desde?.Date,
                Hasta = hasta?.Date,
                ProveedorId = proveedorId,
                CodigoClienteId = codigoClienteId,
                TotalComprado = bloque.TotalComprado,
                TotalPagado = bloque.TotalPagado,
                Saldo = bloque.Saldo,
                PorProveedor = deudas
                    .OrderBy(d => d.ProveedorNombre, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(d => d.Codigo)
                    .Select(d => new ReportePagoProveedorCodigoNavDto
                    {
                        ProveedorId = d.ProveedorId,
                        ProveedorNombre = d.ProveedorNombre,
                        CodigoClienteId = d.CodigoClienteId,
                        Codigo = d.Codigo,
                        CodigoTitular = d.CodigoTitular,
                        TotalPedidos = d.TotalPedidos,
                        TotalCompras = d.TotalCompras,
                        TotalComprado = d.TotalDeuda,
                        Pagado = d.TotalPagado,
                        Saldo = d.Saldo
                    })
                    .ToList(),
                TotalPagosListados = bloque.UltimosPagos.Sum(p => p.Monto),
                Pagos = bloque.UltimosPagos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el reporte de pagos a proveedores");
            throw;
        }
    }
}
