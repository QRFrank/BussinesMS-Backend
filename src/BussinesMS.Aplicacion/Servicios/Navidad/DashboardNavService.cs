using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Dashboard navideño por rol (solo lectura, temporada abierta). Reutiliza los cálculos de los servicios existentes
// (deudas de proveedores, faltantes, deuda de ruta, sueldos y reporte del módulo 8, resumen de capital, estado de conteo,
// me-deben); lo nuevo son agregados en SQL con AsNoTracking. Reutiliza los helpers internal de VentaNavService (permisos y nombres).
public class DashboardNavService : IDashboardNavService
{
    private const int UmbralStockBajoDefecto = 10;
    private const int MaxFilasLista = 30;

    private readonly VentaNavService _ventas;
    private readonly IAbastecimientoNavService _abastecimiento;
    private readonly IRutaNavService _ruta;
    private readonly IPagoVendedorNavService _pagosVendedores;
    private readonly IAporteCapitalService _capital;
    private readonly IConteoNavService _conteo;
    private readonly IVentaClienteRutaNavService _misVentas;
    private readonly IAlmacenAccesoNavService _acceso;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly ITemporadaRepository _temporadaRepo;
    private readonly IVentaNavRepository _ventaRepo;
    private readonly IRecepcionNavRepository _recepcionRepo;
    private readonly ICompraNavRepository _compraRepo;
    private readonly IStockAlmacenNavRepository _stockRepo;
    private readonly ITrasladoNavRepository _trasladoRepo;
    private readonly IDevolucionRutaNavRepository _devolucionRepo;
    private readonly ISalidaRutaNavRepository _salidaRepo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly IConsumoNavRepository _consumoRepo;
    private readonly IConteoInventarioNavRepository _conteoRepo;
    private readonly ResumenNavCalculos _calc;
    private readonly IStockNavService _stockService;
    private readonly ILogger<DashboardNavService> _logger;

    public DashboardNavService(
        VentaNavService ventas,
        IAbastecimientoNavService abastecimiento,
        IRutaNavService ruta,
        IPagoVendedorNavService pagosVendedores,
        IAporteCapitalService capital,
        IConteoNavService conteo,
        IVentaClienteRutaNavService misVentas,
        IAlmacenAccesoNavService acceso,
        ITemporadaActualService temporadaActual,
        ITemporadaRepository temporadaRepo,
        IVentaNavRepository ventaRepo,
        IRecepcionNavRepository recepcionRepo,
        ICompraNavRepository compraRepo,
        IStockAlmacenNavRepository stockRepo,
        ITrasladoNavRepository trasladoRepo,
        IDevolucionRutaNavRepository devolucionRepo,
        ISalidaRutaNavRepository salidaRepo,
        IProductoNavRepository productoRepo,
        IAlmacenRepository almacenRepo,
        IConsumoNavRepository consumoRepo,
        IConteoInventarioNavRepository conteoRepo,
        ResumenNavCalculos calc,
        IStockNavService stockService,
        ILogger<DashboardNavService> logger)
    {
        _ventas = ventas;
        _abastecimiento = abastecimiento;
        _ruta = ruta;
        _pagosVendedores = pagosVendedores;
        _capital = capital;
        _conteo = conteo;
        _misVentas = misVentas;
        _acceso = acceso;
        _temporadaActual = temporadaActual;
        _temporadaRepo = temporadaRepo;
        _ventaRepo = ventaRepo;
        _recepcionRepo = recepcionRepo;
        _compraRepo = compraRepo;
        _stockRepo = stockRepo;
        _trasladoRepo = trasladoRepo;
        _devolucionRepo = devolucionRepo;
        _salidaRepo = salidaRepo;
        _productoRepo = productoRepo;
        _almacenRepo = almacenRepo;
        _consumoRepo = consumoRepo;
        _conteoRepo = conteoRepo;
        _calc = calc;
        _stockService = stockService;
        _logger = logger;
    }

    private static ExcepcionDominio SinPermiso(string mensaje) => new(mensaje, 403, "SIN_PERMISO");

    private static DashboardTemporadaNavDto MapearTemporada(BussinesMS.Dominio.Entidades.Navidad.Temporada t) => new()
    {
        Id = t.Id,
        Nombre = t.Nombre,
        Anio = t.Anio,
        FechaInicio = t.FechaInicio
    };

    // ================= Perfil =================

    public async Task<DashboardPerfilNavDto> ObtenerPerfilAsync()
    {
        try
        {
            var temporada = await _temporadaRepo.ObtenerAbiertaAsync();
            var resultado = new DashboardPerfilNavDto { Temporada = temporada == null ? null : MapearTemporada(temporada) };

            if (await _acceso.EsAdminAsync())
            {
                resultado.Perfil = "admin";
                return resultado;
            }

            if (await _acceso.EsEncargadoAsync())
            {
                resultado.Perfil = "encargado";
                if (temporada != null)
                {
                    resultado.AlmacenesACargo = await _acceso.ObtenerAlmacenesACargoAsync(temporada.Id);
                    // Encargado que además es vendedor: se informa para mostrar también su dashboard de vendedor
                    var (_, vendedorEnc) = await _ventas.ObtenerPermisosAsync(temporada.Id);
                    if (vendedorEnc != null)
                    {
                        var nombresEnc = await _ventas.ObtenerNombresUsuarioAsync(new[] { vendedorEnc.UsuarioId });
                        resultado.VendedorId = vendedorEnc.Id;
                        resultado.VendedorNombre = nombresEnc.GetValueOrDefault(vendedorEnc.UsuarioId, string.Empty);
                        resultado.VendedorTipo = vendedorEnc.Tipo == TipoVendedor.Ruta ? "ruta" : "tienda";
                    }
                }
                return resultado;
            }

            if (temporada != null)
            {
                var (_, vendedor) = await _ventas.ObtenerPermisosAsync(temporada.Id);
                if (vendedor != null)
                {
                    var nombres = await _ventas.ObtenerNombresUsuarioAsync(new[] { vendedor.UsuarioId });
                    resultado.Perfil = vendedor.Tipo == TipoVendedor.Ruta ? "vendedorRuta" : "vendedorTienda";
                    resultado.VendedorId = vendedor.Id;
                    resultado.VendedorNombre = nombres.GetValueOrDefault(vendedor.UsuarioId, string.Empty);
                    resultado.VendedorTipo = vendedor.Tipo == TipoVendedor.Ruta ? "ruta" : "tienda";
                }
            }

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el perfil del dashboard");
            throw;
        }
    }

    // ================= Admin =================

    public async Task<DashboardAdminNavDto> ObtenerAdminAsync()
    {
        try
        {
            if (!await _acceso.EsAdminAsync())
                throw SinPermiso("Solo el admin puede ver el resumen del negocio");

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            var t = temporada.Id;
            var filtro = new AbastecimientoNavFiltroDto();

            // Reutilizados de los servicios existentes
            var deudasProveedor = await _abastecimiento.ObtenerDeudasAsync(filtro);
            var faltantes = await _abastecimiento.ObtenerFaltantesAsync(filtro);
            var deudasRuta = await _ruta.ObtenerDeudasAsync(null);
            var sueldos = await _pagosVendedores.ObtenerResumenSueldosAsync(null, null);
            var reporte = await _pagosVendedores.ObtenerReporteAsync(new ReporteVentasVendedorFiltroDto());
            var capital = await _capital.ObtenerResumenAsync(null);

            var gastos = await _calc.ObtenerGastosAsync(t, MaxFilasLista);

            var resultado = new DashboardAdminNavDto
            {
                Temporada = MapearTemporada(temporada),
                Ventas = await _calc.ObtenerVentasAsync(t),
                Deudas = new DashboardDeudasNavDto
                {
                    Proveedores = _calc.ConstruirDeudaProveedores(deudasProveedor),
                    Clientes = await _calc.ObtenerDeudaClientesAsync(t),
                    VendedoresRuta = _calc.ConstruirDeudaRuta(deudasRuta),
                    Inversores = _calc.ConstruirDeudaInversores(capital),
                    Sueldos = _calc.ConstruirSueldos(sueldos),
                    Comisiones = await _calc.ConstruirComisionesAsync(t, reporte)
                },
                FaltaLlegar = _calc.ConstruirFaltaLlegar(faltantes, incluirCompletos: true),
                PagosProveedores = await _calc.ConstruirPagosProveedoresAsync(t, deudasProveedor),
                Tablas = new DashboardTablasNavDto
                {
                    VentasPorVendedor = _calc.ConstruirVentasPorVendedor(reporte),
                    Gastos = gastos
                },
                Alertas = await ConstruirAlertasAsync(t, deudasRuta),
                ReservasPendientes = await _calc.ObtenerReservasPendientesAsync(t),
                Resultado = await _calc.ObtenerResultadoAsync(t, null, null),
                Capital = await _calc.ObtenerCapitalAsync(t),
                FlujoEfectivo = await _calc.ObtenerFlujoEfectivoAsync(t, null, null),
                NosDeben = await _calc.ObtenerNosDebenAsync(t),
                Debo = await _calc.ObtenerDeboAsync(t),
                Gastos = await _calc.ObtenerGastosTarjetaAsync(t, null, null),
                Perdidas = await _calc.ObtenerPerdidasAsync(t, null, null),
                Conciliacion = await _calc.ObtenerConciliacionAsync(t)
            };

            await LlenarTablasProductosAsync(t, resultado.Tablas);
            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el dashboard del admin");
            throw;
        }
    }

    // Productos más vendidos y recepcionado contra vendido: agregados en SQL por producto
    private async Task LlenarTablasProductosAsync(int temporadaId, DashboardTablasNavDto tablas)
    {
        var vendidos = (await _calc.ObtenerVendidoPorProductoAsync(temporadaId))
            .Select(x => new { x.ProductoId, x.Unidades, Monto = x.Neto })
            .ToList();

        var recepcionados = await _recepcionRepo.AsQueryable().AsNoTracking()
            .Where(r => r.IsActive && !r.Anulada && r.TemporadaId == temporadaId)
            .SelectMany(r => r.Detalles)
            .GroupBy(d => d.ProductoId)
            .Select(g => new { ProductoId = g.Key, Unidades = g.Sum(d => d.CantidadUnidades) })
            .ToListAsync();

        var comprados = await _compraRepo.AsQueryable().AsNoTracking()
            .Where(c => c.IsActive && !c.Anulada && c.TemporadaId == temporadaId)
            .SelectMany(c => c.Detalles)
            .GroupBy(d => d.ProductoId)
            .Select(g => new { ProductoId = g.Key, Unidades = g.Sum(d => d.CantidadUnidades) })
            .ToListAsync();

        var stock = await _stockRepo.AsQueryable().AsNoTracking()
            .Where(s => s.IsActive && s.TemporadaId == temporadaId)
            .GroupBy(s => s.ProductoId)
            .Select(g => new { ProductoId = g.Key, Unidades = g.Sum(s => s.Cantidad) })
            .ToListAsync();

        // Mismo criterio que Pérdidas: consumos activos y no anulados de la temporada
        var consumidos = await _consumoRepo.AsQueryable().AsNoTracking()
            .Where(c => c.IsActive && !c.Anulado && c.TemporadaId == temporadaId)
            .SelectMany(c => c.Detalles)
            .GroupBy(d => d.ProductoId)
            .Select(g => new { ProductoId = g.Key, Unidades = g.Sum(d => d.Unidades) })
            .ToListAsync();

        // Mismo criterio que Pérdidas (diferenciasConteo): conteos activos de la temporada, solo diferencias negativas, todos los motivos
        var perdidasConteo = await _conteoRepo.AsQueryable().AsNoTracking()
            .Where(c => c.IsActive && c.TemporadaId == temporadaId)
            .SelectMany(c => c.Detalles)
            .Where(d => d.Diferencia < 0)
            .GroupBy(d => d.ProductoId)
            .Select(g => new { ProductoId = g.Key, Unidades = -g.Sum(d => d.Diferencia) })
            .ToListAsync();

        var ids = vendidos.Select(x => x.ProductoId)
            .Concat(recepcionados.Select(x => x.ProductoId))
            .Concat(comprados.Select(x => x.ProductoId))
            .Concat(consumidos.Select(x => x.ProductoId))
            .Concat(perdidasConteo.Select(x => x.ProductoId))
            .Distinct()
            .ToList();
        var productos = await ObtenerProductosAsync(ids);

        tablas.ProductosMasVendidos = vendidos
            .OrderByDescending(x => x.Unidades).ThenByDescending(x => x.Monto)
            .Take(10)
            .Select(x =>
            {
                productos.TryGetValue(x.ProductoId, out var p);
                return new DashboardProductoVendidoNavDto
                {
                    ProductoId = x.ProductoId,
                    ProductoNombreMostrar = p?.NombreMostrar ?? string.Empty,
                    UnidadesPorEmpaque = p?.UnidadesPorEmpaque,
                    NombreEmpaque = p?.NombreEmpaque,
                    Color = p?.Color,
                    Unidades = x.Unidades,
                    Monto = Math.Round(x.Monto, 2, MidpointRounding.AwayFromZero)
                };
            })
            .ToList();

        var vendidoPor = vendidos.ToDictionary(x => x.ProductoId, x => x.Unidades);
        var recepcionadoPor = recepcionados.ToDictionary(x => x.ProductoId, x => x.Unidades);
        var compradoPor = comprados.ToDictionary(x => x.ProductoId, x => x.Unidades);
        var stockPor = stock.ToDictionary(x => x.ProductoId, x => x.Unidades);
        var consumoPor = consumidos.ToDictionary(x => x.ProductoId, x => x.Unidades);
        var perdidaConteoPor = perdidasConteo.ToDictionary(x => x.ProductoId, x => x.Unidades);

        tablas.RecepcionadoVsVendido = ids
            .Select(id =>
            {
                productos.TryGetValue(id, out var p);
                return new DashboardRecepcionadoVendidoNavDto
                {
                    ProductoId = id,
                    ProductoNombreMostrar = p?.NombreMostrar ?? string.Empty,
                    UnidadesPorEmpaque = p?.UnidadesPorEmpaque,
                    NombreEmpaque = p?.NombreEmpaque,
                    Color = p?.Color,
                    Recepcionado = recepcionadoPor.GetValueOrDefault(id),
                    Comprado = compradoPor.GetValueOrDefault(id),
                    Vendido = vendidoPor.GetValueOrDefault(id),
                    EnStock = stockPor.GetValueOrDefault(id),
                    Consumo = consumoPor.GetValueOrDefault(id),
                    PerdidaConteo = perdidaConteoPor.GetValueOrDefault(id)
                };
            })
            .OrderBy(x => x.ProductoNombreMostrar, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private async Task<DashboardAlertasNavDto> ConstruirAlertasAsync(int temporadaId, List<DeudaVendedorRutaDto> deudasRuta)
    {
        var alertas = new DashboardAlertasNavDto();

        // Tiendas con conteo: estado de hoy y caja pendiente (mismo cálculo que GET /Conteos/estado)
        var tiendas = (await _temporadaRepo.ObtenerAlmacenesConteoAsync(new[] { temporadaId })).Select(a => a.AlmacenId).Distinct().ToList();
        var estados = await ObtenerEstadosConteoAsync(tiendas);
        alertas.Conteos = estados.Where(e => e.Estado != "cerrada").ToList();
        alertas.CajasSinEntregar = estados.Where(e => e.CajaPendiente).ToList();

        // Salidas de ruta abiertas
        var abiertas = await _salidaRepo.AsQueryable().AsNoTracking()
            .Where(s => s.IsActive && s.TemporadaId == temporadaId && s.Estado == EstadoSalidaRuta.Abierta)
            .OrderBy(s => s.FechaApertura)
            .Select(s => new { s.Id, s.VendedorId, s.FechaApertura, UsuarioId = s.Vendedor != null ? s.Vendedor.UsuarioId : 0 })
            .ToListAsync();
        var nombres = await _ventas.ObtenerNombresUsuarioAsync(abiertas.Select(s => s.UsuarioId));
        alertas.SalidasAbiertas = abiertas
            .Select(s => new DashboardSalidaAbiertaNavDto
            {
                SalidaId = s.Id,
                VendedorId = s.VendedorId,
                VendedorNombre = nombres.GetValueOrDefault(s.UsuarioId, string.Empty),
                FechaApertura = s.FechaApertura,
                UnidadesEnPoder = deudasRuta.FirstOrDefault(d => d.VendedorId == s.VendedorId)?.UnidadesEnPoder ?? 0
            })
            .ToList();

        return alertas;
    }

    // Estado de conteo de cada tienda que lleva apertura y cierre diario
    private async Task<List<DashboardConteoTiendaNavDto>> ObtenerEstadosConteoAsync(List<int> almacenIds)
    {
        if (almacenIds.Count == 0) return new List<DashboardConteoTiendaNavDto>();

        var nombres = await _ventas.ObtenerNombresAlmacenAsync(almacenIds);
        var lista = new List<DashboardConteoTiendaNavDto>();
        foreach (var id in almacenIds)
        {
            var estado = await _conteo.ObtenerEstadoAsync(id, null);
            if (!estado.RequiereConteo) continue;
            lista.Add(MapearConteo(estado, nombres.GetValueOrDefault(id, $"#{id}")));
        }
        return lista.OrderBy(c => c.AlmacenNombre, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static DashboardConteoTiendaNavDto MapearConteo(ConteoEstadoNavDto e, string nombre) => new()
    {
        AlmacenId = e.AlmacenId,
        AlmacenNombre = nombre,
        Estado = e.CierrePendienteFecha.HasValue ? "cierrePendiente"
            : !e.AperturaHecha ? "sinApertura"
            : e.CierreHecho ? "cerrada"
            : "abierta",
        CierrePendienteFecha = e.CierrePendienteFecha,
        CajaPendiente = e.CajaPendiente
    };

    // ================= Vendedor =================

    public async Task<DashboardVendedorNavDto> ObtenerVendedorAsync(int? almacenId)
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            var (esAdmin, vendedor) = await _ventas.ObtenerPermisosAsync(temporada.Id);
            if (vendedor == null)
                throw SinPermiso("El usuario no tiene un vendedor vinculado en la temporada");

            var nombres = await _ventas.ObtenerNombresUsuarioAsync(new[] { vendedor.UsuarioId });
            var resultado = new DashboardVendedorNavDto
            {
                Perfil = vendedor.Tipo == TipoVendedor.Ruta ? "vendedorRuta" : "vendedorTienda",
                Temporada = MapearTemporada(temporada),
                VendedorId = vendedor.Id,
                VendedorNombre = nombres.GetValueOrDefault(vendedor.UsuarioId, string.Empty)
            };

            if (vendedor.Tipo == TipoVendedor.Ruta)
                await LlenarVendedorRutaAsync(resultado, temporada.Id, vendedor.Id, esAdmin);
            else
                await LlenarVendedorTiendaAsync(resultado, temporada.Id, vendedor.Id, almacenId);

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el dashboard del vendedor");
            throw;
        }
    }

    private async Task LlenarVendedorTiendaAsync(DashboardVendedorNavDto resultado, int temporadaId, int vendedorId, int? almacenId)
    {
        var hoy = DateTime.SpecifyKind(BoliviaTimeZone.Now().Date, DateTimeKind.Unspecified);

        // Lo vendido hoy por él (neto), sin anuladas
        var hoyAgg = await _ventaRepo.AsQueryable().AsNoTracking()
            .Where(v => v.IsActive && !v.Anulada && v.TemporadaId == temporadaId && v.VendedorId == vendedorId
                        && v.Canal == CanalVentaNav.Tienda && v.Fecha == hoy)
            .GroupBy(v => 1)
            .Select(g => new { Cantidad = g.Count(), Monto = g.Sum(x => x.MontoNeto) })
            .FirstOrDefaultAsync();
        resultado.Hoy = new DashboardVendedorHoyNavDto
        {
            Fecha = hoy,
            CantidadVentas = hoyAgg?.Cantidad ?? 0,
            Monto = hoyAgg?.Monto ?? 0m
        };

        // Sus reservas con entrega pendiente
        var reservas = await _ventaRepo.AsQueryable().AsNoTracking()
            .Where(v => v.IsActive && !v.Anulada && v.TemporadaId == temporadaId && v.VendedorId == vendedorId
                        && v.Tipo == TipoVentaNav.Reserva && v.EstadoEntrega != EstadoEntregaVentaNav.Entregado)
            .GroupBy(v => 1)
            .Select(g => new { Cantidad = g.Count(), Saldo = g.Sum(x => x.MontoSaldo) })
            .FirstOrDefaultAsync();

        var tienda = new DashboardVendedorTiendaNavDto
        {
            ReservasPendientesEntrega = reservas?.Cantidad ?? 0,
            ReservasSaldo = reservas?.Saldo ?? 0m
        };

        if (almacenId.HasValue)
        {
            var nombres = await _ventas.ObtenerNombresAlmacenAsync(new[] { almacenId.Value });
            tienda.AlmacenId = almacenId.Value;
            tienda.AlmacenNombre = nombres.GetValueOrDefault(almacenId.Value);

            var estado = await _conteo.ObtenerEstadoAsync(almacenId.Value, null);
            if (estado.RequiereConteo)
            {
                tienda.Conteo = MapearConteo(estado, tienda.AlmacenNombre ?? $"#{almacenId.Value}");
                switch (tienda.Conteo.Estado)
                {
                    case "sinApertura":
                        resultado.Avisos.Add(new DashboardAvisoNavDto { Codigo = "SIN_APERTURA", Mensaje = "No hiciste la apertura de hoy" });
                        break;
                    case "cierrePendiente":
                        resultado.Avisos.Add(new DashboardAvisoNavDto
                        {
                            Codigo = "CIERRE_PENDIENTE",
                            Mensaje = $"Falta el cierre del día {estado.CierrePendienteFecha:dd/MM/yyyy}"
                        });
                        break;
                    case "abierta":
                        resultado.Avisos.Add(new DashboardAvisoNavDto { Codigo = "CIERRE_PENDIENTE", Mensaje = "Falta el cierre" });
                        break;
                }
                if (estado.CajaPendiente)
                    resultado.Avisos.Add(new DashboardAvisoNavDto { Codigo = "CAJA_PENDIENTE", Mensaje = "Tenés caja pendiente de entregar" });
            }
        }

        if (tienda.ReservasPendientesEntrega > 0)
            resultado.Avisos.Add(new DashboardAvisoNavDto
            {
                Codigo = "RESERVAS_PENDIENTES",
                Mensaje = tienda.ReservasPendientesEntrega == 1
                    ? "Tenés 1 reserva pendiente de entrega"
                    : $"Tenés {tienda.ReservasPendientesEntrega} reservas pendientes de entrega"
            });

        resultado.Tienda = tienda;
    }

    private async Task LlenarVendedorRutaAsync(DashboardVendedorNavDto resultado, int temporadaId, int vendedorId, bool esAdmin)
    {
        // Deuda con el negocio y salidas abiertas: mismo cálculo que GET /Ruta/deudas (el vendedor ve solo lo suyo)
        var deuda = (await _ruta.ObtenerDeudasAsync(null)).FirstOrDefault(d => d.VendedorId == vendedorId);
        var masAntigua = await _salidaRepo.AsQueryable().AsNoTracking()
            .Where(s => s.IsActive && s.TemporadaId == temporadaId && s.VendedorId == vendedorId && s.Estado == EstadoSalidaRuta.Abierta)
            .Select(s => (DateTime?)s.FechaApertura)
            .MinAsync();

        // Registro personal y privado: solo el propio vendedor (el servicio responde 403 al admin)
        var meDeben = esAdmin ? null : await _misVentas.ObtenerMeDebenAsync(null);

        var ruta = new DashboardVendedorRutaNavDto
        {
            SalidasAbiertas = deuda?.SalidasAbiertas ?? 0,
            UnidadesEnPoder = deuda?.UnidadesEnPoder ?? 0,
            FechaAperturaMasAntigua = masAntigua,
            MontoVendido = deuda?.MontoVendido ?? 0m,
            MontoCobrado = deuda?.MontoCobrado ?? 0m,
            DebeRendir = deuda?.Saldo ?? 0m,
            MeDebenTotal = meDeben?.TotalGeneral ?? 0m,
            MeDebenClientes = meDeben?.Clientes.Count ?? 0
        };

        if (ruta.SalidasAbiertas > 0)
            resultado.Avisos.Add(new DashboardAvisoNavDto
            {
                Codigo = "SALIDAS_ABIERTAS",
                Mensaje = ruta.SalidasAbiertas == 1 ? "Tenés una salida abierta sin cerrar" : $"Tenés {ruta.SalidasAbiertas} salidas abiertas sin cerrar"
            });
        if (ruta.DebeRendir > 0)
            resultado.Avisos.Add(new DashboardAvisoNavDto
            {
                Codigo = "DEUDA_RUTA",
                Mensaje = $"Tenés Bs {ruta.DebeRendir:0.00} por rendir al negocio"
            });

        resultado.Ruta = ruta;
    }

    // ================= Encargado =================

    public async Task<DashboardEncargadoNavDto> ObtenerEncargadoAsync(int? umbralStockBajo)
    {
        try
        {
            if (!await _acceso.EsEncargadoAsync())
                throw SinPermiso("Solo el encargado de almacén puede ver este resumen");

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            var t = temporada.Id;
            var mine = await _acceso.ObtenerAlmacenesACargoAsync(t);
            var umbral = umbralStockBajo.HasValue && umbralStockBajo.Value > 0 ? umbralStockBajo.Value : UmbralStockBajoDefecto;
            var hoy = DateTime.SpecifyKind(BoliviaTimeZone.Now().Date, DateTimeKind.Unspecified);

            var resultado = new DashboardEncargadoNavDto
            {
                Temporada = MapearTemporada(temporada),
                Stock = new DashboardEncargadoStockNavDto { UmbralStockBajo = umbral },
                MovimientosHoy = new DashboardMovimientosHoyNavDto { Fecha = hoy },
                PorLlegar = new DashboardPorLlegarNavDto()
            };

            if (mine.Count == 0)
            {
                resultado.Avisos.Add(new DashboardAvisoNavDto
                {
                    Codigo = "SIN_ALMACENES",
                    Mensaje = "No tenés almacenes asignados en esta temporada"
                });
                return resultado;
            }

            await LlenarEncargadoStockAsync(resultado, t, mine, umbral);
            await LlenarEncargadoPorLlegarAsync(resultado);
            await LlenarEncargadoMovimientosAsync(resultado, t, mine, hoy);

            resultado.Conteo = await ObtenerEstadosConteoAsync(mine);
            foreach (var c in resultado.Conteo)
            {
                switch (c.Estado)
                {
                    case "sinApertura":
                        resultado.Avisos.Add(new DashboardAvisoNavDto { Codigo = "SIN_APERTURA", Mensaje = $"{c.AlmacenNombre}: falta la apertura de hoy" });
                        break;
                    case "cierrePendiente":
                        resultado.Avisos.Add(new DashboardAvisoNavDto
                        {
                            Codigo = "CIERRE_PENDIENTE",
                            Mensaje = $"{c.AlmacenNombre}: falta el cierre del día {c.CierrePendienteFecha:dd/MM/yyyy}"
                        });
                        break;
                    case "abierta":
                        resultado.Avisos.Add(new DashboardAvisoNavDto { Codigo = "CIERRE_PENDIENTE", Mensaje = $"{c.AlmacenNombre}: falta el cierre de hoy" });
                        break;
                }
                if (c.CajaPendiente)
                    resultado.Avisos.Add(new DashboardAvisoNavDto { Codigo = "CAJA_PENDIENTE", Mensaje = $"{c.AlmacenNombre}: caja pendiente de entregar" });
            }

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el dashboard del encargado");
            throw;
        }
    }

    private async Task LlenarEncargadoStockAsync(DashboardEncargadoNavDto resultado, int temporadaId, List<int> mine, int umbral)
    {
        var almacenes = await _almacenRepo.AsQueryable().AsNoTracking()
            .Where(a => mine.Contains(a.Id))
            .Select(a => new { a.Id, a.Nombre, a.EsTienda })
            .ToListAsync();

        var filas = await _stockRepo.AsQueryable().AsNoTracking()
            .Where(s => s.IsActive && s.TemporadaId == temporadaId && mine.Contains(s.AlmacenId))
            .Select(s => new { s.AlmacenId, s.ProductoId, s.Cantidad })
            .ToListAsync();

        resultado.Almacenes = almacenes
            .OrderBy(a => a.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .Select(a => new DashboardAlmacenStockNavDto
            {
                AlmacenId = a.Id,
                AlmacenNombre = a.Nombre,
                EsTienda = a.EsTienda,
                Unidades = filas.Where(f => f.AlmacenId == a.Id).Sum(f => f.Cantidad)
            })
            .ToList();
        resultado.Stock.TotalUnidades = filas.Sum(f => f.Cantidad);

        // Por producto, sumando sus almacenes. Solo productos activos
        var porProducto = filas.GroupBy(f => f.ProductoId).ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad));
        var activos = await _productoRepo.AsQueryable().AsNoTracking()
            .Where(p => p.IsActive && porProducto.Keys.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();
        var activosSet = activos.ToHashSet();
        var agotadosIds = porProducto.Where(kv => activosSet.Contains(kv.Key) && kv.Value <= 0).Select(kv => kv.Key).ToList();
        var pocosIds = porProducto.Where(kv => activosSet.Contains(kv.Key) && kv.Value > 0 && kv.Value <= umbral)
            .OrderBy(kv => kv.Value).Select(kv => kv.Key).ToList();

        resultado.Stock.CantidadAgotados = agotadosIds.Count;
        resultado.Stock.CantidadPocoStock = pocosIds.Count;

        var mostrar = agotadosIds.Take(MaxFilasLista).Concat(pocosIds.Take(MaxFilasLista)).ToList();
        var productos = await ObtenerProductosAsync(mostrar);

        resultado.Stock.Agotados = agotadosIds.Take(MaxFilasLista)
            .Select(id => MapearProductoUnidades(id, 0, productos))
            .OrderBy(x => x.ProductoNombreMostrar, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        resultado.Stock.PocoStock = pocosIds.Take(MaxFilasLista)
            .Select(id => MapearProductoUnidades(id, porProducto[id], productos))
            .ToList();

        // Lista completa (mismo universo: productos activos con registro de stock en sus almacenes) con bloqueo por reservas
        var bloqueosPorAlmacenProducto = await _stockService.ObtenerBloqueadosAsync(temporadaId);
        var bloqueadoPor = bloqueosPorAlmacenProducto
            .Where(kv => mine.Contains(kv.Key.AlmacenId))
            .GroupBy(kv => kv.Key.ProductoId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Value));

        var todosIds = porProducto.Keys.Where(activosSet.Contains).ToList();
        var todos = await ObtenerProductosAsync(todosIds);
        var lista = todosIds.Select(id =>
        {
            var unidades = porProducto[id];
            var bloqueado = bloqueadoPor.GetValueOrDefault(id);
            var info = MapearProductoUnidades(id, unidades, todos);
            return new DashboardEncargadoProductoStockNavDto
            {
                ProductoId = info.ProductoId,
                ProductoNombreMostrar = info.ProductoNombreMostrar,
                UnidadesPorEmpaque = info.UnidadesPorEmpaque,
                NombreEmpaque = info.NombreEmpaque,
                Color = info.Color,
                Unidades = unidades,
                Bloqueado = bloqueado,
                Disponible = Math.Max(0, unidades - bloqueado),
                Estado = unidades <= 0 ? "Agotado" : unidades <= umbral ? "PocoStock" : "Normal"
            };
        }).ToList();

        resultado.Stock.Productos = lista
            .OrderBy(x => x.Estado == "Agotado" ? 0 : x.Estado == "PocoStock" ? 1 : 2)
            .ThenBy(x => x.Estado == "PocoStock" ? x.Unidades : 0)
            .ThenBy(x => x.ProductoNombreMostrar, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        resultado.Bloqueados = new DashboardEncargadoBloqueadosNavDto
        {
            CantidadProductos = lista.Count(x => x.Bloqueado > 0),
            TotalUnidades = lista.Sum(x => x.Bloqueado)
        };
    }

    // El pedido no distribuye por almacén: se informa lo que falta llegar de toda la temporada, por producto
    private async Task LlenarEncargadoPorLlegarAsync(DashboardEncargadoNavDto resultado)
    {
        var faltantes = (await _abastecimiento.ObtenerFaltantesAsync(new AbastecimientoNavFiltroDto())).Where(f => f.Faltante > 0).ToList();
        var porProducto = faltantes
            .GroupBy(f => f.ProductoId)
            .Select(g => new
            {
                ProductoId = g.Key,
                Faltante = g.Sum(x => x.Faltante),
                Nombre = g.First().ProductoNombreMostrar,
                UxE = g.First().UnidadesPorEmpaque,
                Empaque = g.First().NombreEmpaque
            })
            .OrderByDescending(x => x.Faltante)
            .ToList();

        var ids = porProducto.Take(MaxFilasLista).Select(x => x.ProductoId).ToList();
        var productos = await ObtenerProductosAsync(ids);

        resultado.PorLlegar = new DashboardPorLlegarNavDto
        {
            DistribuidoPorAlmacen = false,
            Nota = "El pedido no se distribuye por almacén: se muestra lo que falta llegar de toda la temporada",
            TotalUnidades = porProducto.Sum(x => x.Faltante),
            Items = porProducto.Take(MaxFilasLista)
                .Select(x => new DashboardProductoUnidadesNavDto
                {
                    ProductoId = x.ProductoId,
                    ProductoNombreMostrar = x.Nombre,
                    UnidadesPorEmpaque = x.UxE,
                    NombreEmpaque = x.Empaque,
                    Color = productos.GetValueOrDefault(x.ProductoId)?.Color,
                    Unidades = x.Faltante
                })
                .ToList()
        };
    }

    // Documentos de hoy que tocan sus almacenes (no anulados)
    private async Task LlenarEncargadoMovimientosAsync(DashboardEncargadoNavDto resultado, int temporadaId, List<int> mine, DateTime hoy)
    {
        var manana = hoy.AddDays(1);

        var recepciones = await _recepcionRepo.AsQueryable().AsNoTracking()
            .Where(r => r.IsActive && !r.Anulada && r.TemporadaId == temporadaId && r.Fecha >= hoy && r.Fecha < manana)
            .Select(r => r.Detalles.SelectMany(d => d.Distribuciones).Where(x => mine.Contains(x.AlmacenId)).Sum(x => (int?)x.CantidadUnidades) ?? 0)
            .ToListAsync();
        resultado.MovimientosHoy.Recepciones = Resumir(recepciones);

        var entrantes = await _trasladoRepo.AsQueryable().AsNoTracking()
            .Where(x => x.IsActive && !x.Anulado && x.TemporadaId == temporadaId && x.Fecha >= hoy && x.Fecha < manana && mine.Contains(x.AlmacenDestinoId))
            .Select(x => x.Detalles.Sum(d => (int?)d.CantidadUnidades) ?? 0)
            .ToListAsync();
        resultado.MovimientosHoy.TrasladosEntrantes = Resumir(entrantes);

        var salientes = await _trasladoRepo.AsQueryable().AsNoTracking()
            .Where(x => x.IsActive && !x.Anulado && x.TemporadaId == temporadaId && x.Fecha >= hoy && x.Fecha < manana && mine.Contains(x.AlmacenOrigenId))
            .Select(x => x.Detalles.Sum(d => (int?)d.CantidadUnidades) ?? 0)
            .ToListAsync();
        resultado.MovimientosHoy.TrasladosSalientes = Resumir(salientes);

        var devoluciones = await _devolucionRepo.AsQueryable().AsNoTracking()
            .Where(d => d.IsActive && !d.Anulada && d.Fecha >= hoy && d.Fecha < manana
                        && d.SalidaRuta != null && d.SalidaRuta.TemporadaId == temporadaId)
            .Select(d => d.Detalles.Where(x => mine.Contains(x.AlmacenDestinoId)).Sum(x => (int?)x.Unidades) ?? 0)
            .ToListAsync();
        resultado.MovimientosHoy.DevolucionesRuta = Resumir(devoluciones);
    }

    private static DashboardMovimientoResumenNavDto Resumir(List<int> unidadesPorDocumento) => new()
    {
        Operaciones = unidadesPorDocumento.Count(u => u > 0),
        Unidades = unidadesPorDocumento.Sum()
    };

    // ================= Auxiliares =================

    private sealed class ProductoInfo
    {
        public int Id { get; set; }
        public string NombreMostrar { get; set; } = string.Empty;
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
                UnidadesPorEmpaque = p.UnidadesPorEmpaque,
                NombreEmpaque = p.NombreEmpaque,
                Color = p.Color
            })
            .ToListAsync();
        return lista.ToDictionary(p => p.Id);
    }

    private static DashboardProductoUnidadesNavDto MapearProductoUnidades(int id, int unidades, Dictionary<int, ProductoInfo> productos)
    {
        productos.TryGetValue(id, out var p);
        return new DashboardProductoUnidadesNavDto
        {
            ProductoId = id,
            ProductoNombreMostrar = p?.NombreMostrar ?? string.Empty,
            UnidadesPorEmpaque = p?.UnidadesPorEmpaque,
            NombreEmpaque = p?.NombreEmpaque,
            Color = p?.Color,
            Unidades = unidades
        };
    }
}
