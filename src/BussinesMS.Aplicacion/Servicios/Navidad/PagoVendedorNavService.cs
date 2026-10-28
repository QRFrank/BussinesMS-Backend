using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using static BussinesMS.Aplicacion.Servicios.Navidad.VentaNavService;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Pagos a vendedores (módulo 8): reporte de ventas por vendedor (Ruta + Tienda por VentaNav.VendedorId, neto con descuento prorrateado),
// pago de comisión (una vez por temporada, calculada en el servidor) y sueldos mensuales. El admin lo hace todo; el vendedor ve solo lo suyo.
// No toca Producto ni la deuda del vendedor. Reutiliza los helpers internal de VentaNavService (permisos, nombres, productos).
public class PagoVendedorNavService : IPagoVendedorNavService
{
    private readonly VentaNavService _ventas;
    private readonly IPagoVendedorNavRepository _repo;
    private readonly IVentaNavRepository _ventaRepo;
    private readonly IVendedorNavRepository _vendedorRepo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly ITemporadaRepository _temporadaRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PagoVendedorNavService> _logger;

    public PagoVendedorNavService(
        VentaNavService ventas,
        IPagoVendedorNavRepository repo,
        IVentaNavRepository ventaRepo,
        IVendedorNavRepository vendedorRepo,
        IProductoNavRepository productoRepo,
        ITemporadaRepository temporadaRepo,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        ICurrentUserService currentUser,
        ILogger<PagoVendedorNavService> logger)
    {
        _ventas = ventas;
        _repo = repo;
        _ventaRepo = ventaRepo;
        _vendedorRepo = vendedorRepo;
        _productoRepo = productoRepo;
        _temporadaRepo = temporadaRepo;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    // ================= Permisos =================

    private static ExcepcionDominio SinPermiso(string mensaje = "No tiene permiso para realizar esta acción")
        => new(mensaje, 403, "SIN_PERMISO");

    // Lecturas: admin ve todo (null); el vendedor de la temporada solo lo suyo (su id); el resto, 403
    private async Task<int?> RestriccionLecturaAsync(int temporadaId)
    {
        var (esAdmin, vendedor) = await _ventas.ObtenerPermisosAsync(temporadaId);
        if (esAdmin) return null;
        if (vendedor != null) return vendedor.Id;
        throw SinPermiso();
    }

    private async Task ExigirAdminAsync(int temporadaId)
    {
        var (esAdmin, _) = await _ventas.ObtenerPermisosAsync(temporadaId);
        if (!esAdmin) throw SinPermiso("Solo el admin puede realizar esta acción");
    }

    // ================= Contexto =================

    public async Task<PagoVendedorContextoDto> ObtenerContextoAsync()
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            var (esAdmin, vendedorActual) = await _ventas.ObtenerPermisosAsync(temporada.Id);
            if (!esAdmin && vendedorActual == null) throw SinPermiso();

            var query = _vendedorRepo.AsQueryable().AsNoTracking()
                .Where(v => v.TemporadaId == temporada.Id && v.IsActive);
            if (!esAdmin)
            {
                var propio = vendedorActual!.Id;
                query = query.Where(v => v.Id == propio);
            }
            var vendedores = await query.ToListAsync();
            var nombres = await _ventas.ObtenerNombresUsuarioAsync(vendedores.Select(v => v.UsuarioId));

            return new PagoVendedorContextoDto
            {
                EsAdmin = esAdmin,
                VendedorId = vendedorActual?.Id,
                PuedeVerTodo = esAdmin,
                PuedeRegistrar = esAdmin,
                Vendedores = vendedores
                    .Select(v => new PagoVendedorVendedorItemDto
                    {
                        Id = v.Id,
                        Nombre = nombres.GetValueOrDefault(v.UsuarioId, string.Empty),
                        Tipo = (int)v.Tipo,
                        TipoNombre = NombreTipoVendedor(v.Tipo),
                        SueldoMensual = v.SueldoMensual
                    })
                    .OrderBy(v => v.Nombre, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el contexto de pagos a vendedores");
            throw;
        }
    }

    // ================= Reporte =================

    public async Task<List<ReporteVentasVendedorDto>> ObtenerReporteAsync(ReporteVentasVendedorFiltroDto filtro)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(filtro.TemporadaId);
            var forzado = await RestriccionLecturaAsync(temporadaId);
            var vendedorId = forzado ?? filtro.VendedorId;

            if (filtro.Canal.HasValue && !Enum.IsDefined(typeof(CanalVentaNav), filtro.Canal.Value))
                throw new ValidacionException("El canal es inválido (1 Tienda, 2 Ruta)");
            if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value.Date > filtro.Hasta.Value.Date)
                throw new ValidacionException("La fecha inicial no puede ser mayor a la final");

            var query = _vendedorRepo.AsQueryable().AsNoTracking().Where(v => v.TemporadaId == temporadaId);
            query = vendedorId.HasValue ? query.Where(v => v.Id == vendedorId.Value) : query.Where(v => v.IsActive);
            var vendedores = await query.ToListAsync();
            if (vendedorId.HasValue && vendedores.Count == 0)
                throw new ValidacionException("El vendedor no existe o no es de la temporada");

            return await ConstruirReportesAsync(temporadaId, vendedores, (CanalVentaNav?)filtro.Canal, filtro.Desde, filtro.Hasta, forzado == null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el reporte de ventas por vendedor");
            throw;
        }
    }

    private async Task<List<ReporteVentasVendedorDto>> ConstruirReportesAsync(
        int temporadaId, List<Vendedor> vendedores, CanalVentaNav? canal, DateTime? desde, DateTime? hasta, bool incluirCobranza)
    {
        var vendedorIds = vendedores.Select(v => v.Id).ToList();
        var nombres = await _ventas.ObtenerNombresUsuarioAsync(vendedores.Select(v => v.UsuarioId));

        var query = _ventaRepo.AsQueryable().AsNoTracking()
            .Where(v => v.IsActive && !v.Anulada && v.TemporadaId == temporadaId && vendedorIds.Contains(v.VendedorId));
        if (canal.HasValue)
            query = query.Where(v => v.Canal == canal.Value);
        if (desde.HasValue)
        {
            var d = desde.Value.Date;
            query = query.Where(v => v.Fecha >= d);
        }
        if (hasta.HasValue)
        {
            var h = hasta.Value.Date.AddDays(1);
            query = query.Where(v => v.Fecha < h);
        }

        var ventas = await query
            .Select(v => new
            {
                v.VendedorId,
                v.Canal,
                v.MontoTotal,
                v.Descuento,
                v.MontoNeto,
                v.MontoCobrado,
                v.MontoSaldo,
                Lineas = v.Detalles.Where(d => d.Unidades > 0).Select(d => new { d.ProductoId, d.Unidades, d.Subtotal }).ToList()
            })
            .ToListAsync();

        var productoIds = ventas.SelectMany(v => v.Lineas).Select(l => l.ProductoId).Distinct().ToList();
        var productos = productoIds.Count == 0
            ? new Dictionary<int, Producto>()
            : await _productoRepo.AsQueryable().AsNoTracking().Where(p => productoIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var comisiones = await _repo.AsQueryable().AsNoTracking()
            .Where(p => p.IsActive && !p.Anulado && p.TemporadaId == temporadaId && p.Tipo == TipoPagoVendedor.Comision && vendedorIds.Contains(p.VendedorId))
            .Select(p => new { p.VendedorId, p.Id })
            .ToListAsync();

        var resultado = new List<ReporteVentasVendedorDto>();
        foreach (var vendedor in vendedores)
        {
            var suyas = ventas.Where(v => v.VendedorId == vendedor.Id).ToList();
            var filas = new Dictionary<int, (int Ruta, int Tienda, decimal Bruto, decimal Neto)>();
            foreach (var venta in suyas)
            {
                // Parte proporcional del descuento de la venta en cada línea
                var factor = venta.MontoTotal > 0 ? venta.MontoNeto / venta.MontoTotal : 1m;
                foreach (var l in venta.Lineas)
                {
                    var f = filas.GetValueOrDefault(l.ProductoId);
                    filas[l.ProductoId] = (
                        f.Ruta + (venta.Canal == CanalVentaNav.Ruta ? l.Unidades : 0),
                        f.Tienda + (venta.Canal == CanalVentaNav.Tienda ? l.Unidades : 0),
                        f.Bruto + l.Subtotal,
                        f.Neto + l.Subtotal * factor);
                }
            }

            var lista = filas
                .Select(kv =>
                {
                    productos.TryGetValue(kv.Key, out var p);
                    return new ReporteVentasVendedorProductoDto
                    {
                        ProductoId = kv.Key,
                        ProductoNombreMostrar = p != null ? NombreMostrar(p) : string.Empty,
                        UnidadesPorEmpaque = p?.UnidadesPorEmpaque,
                        NombreEmpaque = p?.NombreEmpaque,
                        Color = p?.Color,
                        UnidadesRuta = kv.Value.Ruta,
                        UnidadesTienda = kv.Value.Tienda,
                        Unidades = kv.Value.Ruta + kv.Value.Tienda,
                        MontoBruto = Redondear(kv.Value.Bruto),
                        MontoNeto = Redondear(kv.Value.Neto)
                    };
                })
                .OrderBy(x => x.ProductoNombreMostrar, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            resultado.Add(new ReporteVentasVendedorDto
            {
                VendedorId = vendedor.Id,
                VendedorNombre = nombres.GetValueOrDefault(vendedor.UsuarioId, string.Empty),
                Tipo = (int)vendedor.Tipo,
                TipoNombre = NombreTipoVendedor(vendedor.Tipo),
                Productos = lista,
                Totales = new ReporteVentasVendedorTotalesDto
                {
                    UnidadesRuta = lista.Sum(x => x.UnidadesRuta),
                    UnidadesTienda = lista.Sum(x => x.UnidadesTienda),
                    Unidades = lista.Sum(x => x.Unidades),
                    MontoBruto = suyas.Sum(v => v.MontoTotal),
                    Descuentos = suyas.Sum(v => v.Descuento),
                    MontoNeto = suyas.Sum(v => v.MontoNeto),
                    CantidadVentas = suyas.Count
                },
                MontoCobrado = incluirCobranza ? suyas.Sum(v => v.MontoCobrado) : null,
                Saldo = incluirCobranza ? suyas.Sum(v => v.MontoSaldo) : null,
                PagoComisionVigenteId = comisiones.FirstOrDefault(c => c.VendedorId == vendedor.Id)?.Id
            });
        }

        return resultado.OrderBy(r => r.VendedorNombre, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    // ================= Pagos: lectura =================

    public async Task<PagedResultDto<PagoVendedorDto>> ObtenerPagosAsync(PagoVendedorFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);
            var forzado = await RestriccionLecturaAsync(temporadaId);

            var baseQuery = _repo.AsQueryable()
                .AsNoTracking()
                .Include(p => p.Vendedor)
                .Where(p => p.IsActive && p.TemporadaId == temporadaId);

            if (forzado.HasValue)
                baseQuery = baseQuery.Where(p => p.VendedorId == forzado.Value);
            else if (query.VendedorId.HasValue)
                baseQuery = baseQuery.Where(p => p.VendedorId == query.VendedorId.Value);
            if (query.Tipo.HasValue)
                baseQuery = baseQuery.Where(p => (int)p.Tipo == query.Tipo.Value);
            if (!query.IncluirAnulados)
                baseQuery = baseQuery.Where(p => !p.Anulado);
            if (query.Desde.HasValue)
            {
                var desde = query.Desde.Value.Date;
                baseQuery = baseQuery.Where(p => p.Fecha >= desde);
            }
            if (query.Hasta.HasValue)
            {
                var hastaExclusivo = query.Hasta.Value.Date.AddDays(1);
                baseQuery = baseQuery.Where(p => p.Fecha < hastaExclusivo);
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(p => p.Fecha).ThenByDescending(p => p.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            var nombres = await _ventas.ObtenerNombresUsuarioAsync(
                entidades.Select(p => p.UsuarioId).Concat(entidades.Where(p => p.Vendedor != null).Select(p => p.Vendedor!.UsuarioId)));

            return new PagedResultDto<PagoVendedorDto>
            {
                Items = entidades.Select(p => Mapear(p, nombres)).ToList(),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pagos a vendedores");
            throw;
        }
    }

    public async Task<PagoVendedorDto?> ObtenerPagoAsync(int id)
    {
        try
        {
            var pago = await _repo.ObtenerConDetallesAsync(id);
            if (pago == null || !pago.IsActive) return null;

            var forzado = await RestriccionLecturaAsync(pago.TemporadaId);
            if (forzado.HasValue && forzado.Value != pago.VendedorId)
                throw SinPermiso("Solo puede ver sus propios pagos");

            var nombres = await _ventas.ObtenerNombresUsuarioAsync(
                new[] { pago.UsuarioId }.Concat(pago.Vendedor != null ? new[] { pago.Vendedor.UsuarioId } : Array.Empty<int>()));
            return Mapear(pago, nombres);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el pago a vendedor {Id}", id);
            throw;
        }
    }

    // ================= Pagos: registrar y anular (solo admin) =================

    public async Task<PagoVendedorDto> RegistrarAsync(RegistrarPagoVendedorDto dto)
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            await ExigirAdminAsync(temporada.Id);

            var vendedor = await _vendedorRepo.AsQueryable().AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == dto.VendedorId && v.TemporadaId == temporada.Id && v.IsActive);
            if (vendedor == null)
                throw new ValidacionException("El vendedor no existe, está inactivo o no es de la temporada");

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;
            var pago = new PagoVendedor
            {
                TemporadaId = temporada.Id,
                VendedorId = vendedor.Id,
                Fecha = (dto.Fecha ?? BoliviaTimeZone.Now()).Date,
                Tipo = dto.Tipo,
                Medio = dto.Medio,
                UsuarioId = usuarioId,
                Observacion = NormalizarTexto(dto.Observacion),
                Anulado = false
            };

            if (dto.Tipo == TipoPagoVendedor.Sueldo)
                PrepararSueldo(pago, vendedor, dto);
            else
                await PrepararComisionAsync(pago, vendedor, dto, temporada.Id, usuarioId, ahora);

            await _uow.BeginTransactionAsync();
            try
            {
                await _repo.CrearAsync(pago);
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Pago a vendedor {VendedorId} registrado: {Id}, tipo {Tipo}, monto {Monto}", vendedor.Id, pago.Id, pago.Tipo, pago.Monto);
            return await ObtenerPagoAsync(pago.Id) ?? throw new EntidadNoEncontradaException("Pago a vendedor", pago.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar pago a vendedor");
            throw;
        }
    }

    private static void PrepararSueldo(PagoVendedor pago, Vendedor vendedor, RegistrarPagoVendedorDto dto)
    {
        if (vendedor.Tipo != TipoVendedor.Tienda)
            throw new ValidacionException("El sueldo solo se paga a vendedores de tienda");
        if (string.IsNullOrWhiteSpace(dto.Periodo)
            || !DateTime.TryParseExact(dto.Periodo.Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var periodo))
            throw new ValidacionException("El período es obligatorio (formato año-mes, por ejemplo 2026-11)");
        if (!dto.Monto.HasValue || dto.Monto.Value <= 0)
            throw new ValidacionException("El monto del sueldo debe ser mayor a 0");

        pago.Periodo = periodo.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        pago.Monto = Redondear(dto.Monto.Value);
    }

    private async Task PrepararComisionAsync(PagoVendedor pago, Vendedor vendedor, RegistrarPagoVendedorDto dto, int temporadaId, int usuarioId, DateTime ahora)
    {
        if (vendedor.Tipo != TipoVendedor.Ruta)
            throw new ValidacionException("La comisión solo se paga a vendedores de ruta");
        if (!dto.Modo.HasValue)
            throw new ValidacionException("El modo de comisión es obligatorio (1 Por producto, 2 Porcentaje)");

        var yaPagada = await _repo.AsQueryable().AsNoTracking()
            .AnyAsync(p => p.IsActive && !p.Anulado && p.TemporadaId == temporadaId && p.VendedorId == vendedor.Id && p.Tipo == TipoPagoVendedor.Comision);
        if (yaPagada)
            throw new ExcepcionDominio("El vendedor ya tiene un pago de comisión vigente en la temporada. Anúlelo para registrar otro", 409, "COMISION_YA_PAGADA");

        // Se recalcula en el servidor: toda la temporada, canales Ruta y Tienda
        var reporte = (await ConstruirReportesAsync(temporadaId, new List<Vendedor> { vendedor }, null, null, null, false)).Single();
        if (reporte.Productos.Count == 0)
            throw new ValidacionException("El vendedor no tiene ventas en la temporada");

        pago.ModoComision = dto.Modo;
        pago.MontoBase = reporte.Totales.MontoNeto;

        if (dto.Modo == ModoComisionVendedor.Porcentaje)
        {
            if (!dto.Porcentaje.HasValue || dto.Porcentaje.Value <= 0 || dto.Porcentaje.Value > 100)
                throw new ValidacionException("El porcentaje debe ser mayor a 0 y hasta 100");
            pago.Porcentaje = Math.Round(dto.Porcentaje.Value, 2, MidpointRounding.AwayFromZero);
            pago.Monto = Redondear(reporte.Totales.MontoNeto * pago.Porcentaje.Value / 100m);
        }
        else
        {
            var lineas = dto.Lineas ?? new List<PagoVendedorLineaDto>();
            if (lineas.GroupBy(l => l.ProductoId).Any(g => g.Count() > 1))
                throw new ValidacionException("Hay productos repetidos en las líneas de comisión");
            var vendidos = reporte.Productos.ToDictionary(p => p.ProductoId);
            foreach (var l in lineas)
                if (!vendidos.ContainsKey(l.ProductoId))
                    throw new ValidacionException($"El producto {l.ProductoId} no está entre los vendidos por el vendedor");

            var comisiones = lineas.ToDictionary(l => l.ProductoId, l => l.ComisionUnidad);
            foreach (var p in reporte.Productos)
            {
                var comision = Redondear(comisiones.GetValueOrDefault(p.ProductoId));
                pago.Detalles.Add(new PagoVendedorDetalle
                {
                    ProductoId = p.ProductoId,
                    Unidades = p.Unidades,
                    ComisionUnidad = comision,
                    Subtotal = Redondear(p.Unidades * comision),
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                });
            }
            pago.Monto = pago.Detalles.Sum(d => d.Subtotal);
        }

        if (pago.Monto <= 0)
            throw new ValidacionException("La comisión calculada es 0: indique la comisión por unidad o el porcentaje");
    }

    public async Task<PagoVendedorDto> AnularAsync(int id)
    {
        try
        {
            var pago = await _repo.ObtenerParaEdicionAsync(id);
            if (pago == null || !pago.IsActive)
                throw new EntidadNoEncontradaException("Pago a vendedor", id);
            await ExigirAdminAsync(pago.TemporadaId);
            if (pago.Anulado)
                throw new ValidacionException("El pago ya está anulado");
            await _temporadaActual.VerificarEditableAsync(pago.TemporadaId);

            pago.Anulado = true;
            Tocar(pago, _currentUser.GetUsuarioId() ?? 1, DateTime.UtcNow);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Pago a vendedor {Id} anulado", id);
            return await ObtenerPagoAsync(id) ?? throw new EntidadNoEncontradaException("Pago a vendedor", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular pago a vendedor {Id}", id);
            throw;
        }
    }

    // ================= Sueldos: cuánto se le debe =================

    public async Task<List<SueldoVendedorResumenDto>> ObtenerResumenSueldosAsync(int? temporadaId, int? vendedorId)
    {
        try
        {
            var tempId = await _temporadaActual.ResolverTemporadaIdAsync(temporadaId);
            var forzado = await RestriccionLecturaAsync(tempId);
            var filtro = forzado ?? vendedorId;

            var temporada = await _temporadaRepo.ObtenerPorIdAsync(tempId)
                ?? throw new EntidadNoEncontradaException("Temporada", tempId);
            var hoy = BoliviaTimeZone.Now().Date;
            var fin = temporada.FechaCierre.HasValue && temporada.FechaCierre.Value.Date < hoy ? temporada.FechaCierre.Value.Date : hoy;
            var inicio = temporada.FechaInicio.Date;
            var meses = Math.Max(1, (fin.Year - inicio.Year) * 12 + fin.Month - inicio.Month + 1);

            var query = _vendedorRepo.AsQueryable().AsNoTracking()
                .Where(v => v.TemporadaId == tempId && v.IsActive && v.Tipo == TipoVendedor.Tienda);
            if (filtro.HasValue)
                query = query.Where(v => v.Id == filtro.Value);
            var vendedores = await query.ToListAsync();
            var ids = vendedores.Select(v => v.Id).ToList();
            var nombres = await _ventas.ObtenerNombresUsuarioAsync(vendedores.Select(v => v.UsuarioId));

            var pagos = await _repo.AsQueryable().AsNoTracking()
                .Where(p => p.IsActive && !p.Anulado && p.TemporadaId == tempId && p.Tipo == TipoPagoVendedor.Sueldo && ids.Contains(p.VendedorId))
                .Select(p => new { p.VendedorId, p.Periodo, p.Monto })
                .ToListAsync();

            return vendedores
                .Select(v =>
                {
                    var suyos = pagos.Where(p => p.VendedorId == v.Id).ToList();
                    var pagado = suyos.Sum(p => p.Monto);
                    return new SueldoVendedorResumenDto
                    {
                        VendedorId = v.Id,
                        VendedorNombre = nombres.GetValueOrDefault(v.UsuarioId, string.Empty),
                        SueldoMensual = v.SueldoMensual,
                        Meses = meses,
                        TotalPagado = pagado,
                        Debe = v.SueldoMensual.HasValue ? v.SueldoMensual.Value * meses - pagado : null,
                        Periodos = suyos
                            .GroupBy(p => p.Periodo ?? string.Empty)
                            .Select(g => new SueldoVendedorPeriodoDto { Periodo = g.Key, Pagado = g.Sum(x => x.Monto) })
                            .OrderBy(x => x.Periodo)
                            .ToList()
                    };
                })
                .OrderBy(x => x.VendedorNombre, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el resumen de sueldos");
            throw;
        }
    }

    // ================= Mapeo =================

    private static string NombreTipoVendedor(TipoVendedor t) => t == TipoVendedor.Tienda ? "Tienda" : "Ruta";

    private static PagoVendedorDto Mapear(PagoVendedor p, Dictionary<int, string> nombres) => new()
    {
        Id = p.Id,
        TemporadaId = p.TemporadaId,
        VendedorId = p.VendedorId,
        VendedorNombre = p.Vendedor != null ? nombres.GetValueOrDefault(p.Vendedor.UsuarioId, string.Empty) : string.Empty,
        Fecha = p.Fecha,
        Tipo = (int)p.Tipo,
        TipoNombre = p.Tipo == TipoPagoVendedor.Sueldo ? "Sueldo" : "Comisión",
        Periodo = p.Periodo,
        ModoComision = p.ModoComision.HasValue ? (int)p.ModoComision.Value : null,
        ModoComisionNombre = p.ModoComision switch
        {
            ModoComisionVendedor.PorProducto => "Por producto",
            ModoComisionVendedor.Porcentaje => "Porcentaje",
            _ => null
        },
        Porcentaje = p.Porcentaje,
        MontoBase = p.MontoBase,
        Monto = p.Monto,
        Medio = (int)p.Medio,
        MedioNombre = p.Medio == MedioPagoNav.Transferencia ? "Transferencia" : "Efectivo",
        UsuarioId = p.UsuarioId,
        UsuarioNombre = nombres.GetValueOrDefault(p.UsuarioId, string.Empty),
        Observacion = p.Observacion,
        Anulado = p.Anulado,
        Detalles = p.Detalles
            .Where(d => d.Producto != null)
            .Select(d => new PagoVendedorDetalleDto
            {
                Id = d.Id,
                ProductoId = d.ProductoId,
                ProductoNombreMostrar = NombreMostrar(d.Producto!),
                UnidadesPorEmpaque = d.Producto!.UnidadesPorEmpaque,
                NombreEmpaque = d.Producto.NombreEmpaque,
                Color = d.Producto.Color,
                Unidades = d.Unidades,
                ComisionUnidad = d.ComisionUnidad,
                Subtotal = d.Subtotal
            })
            .OrderBy(d => d.ProductoNombreMostrar, StringComparer.CurrentCultureIgnoreCase)
            .ToList(),
        CreatedAt = BoliviaTimeZone.ToLocal(p.CreatedAt)
    };
}
