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

// Cobertura de reservas. Desde 2026-11-08 es global por producto: Libre = Σ stock de la temporada + porLlegar (misma fuente que
// Faltantes) − Σ pendiente de entregar de las reservas vigentes. Reservar exige Libre ≥ 0; ventas directas y cargas a ruta no pueden
// dejarlo < 0. Se conserva porLlegarLibre (legado: porLlegar − Σ partes sin bloqueo) para no romper el front.
public class ReservaCoberturaNavService : IReservaCoberturaNavService
{
    private readonly IVentaNavRepository _ventaRepo;
    private readonly IReservaBloqueoNavRepository _bloqueoRepo;
    private readonly IStockAlmacenNavRepository _stockRepo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly IAbastecimientoNavService _abastecimiento;
    private readonly IStockNavService _stock;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ReservaCoberturaNavService> _logger;

    public ReservaCoberturaNavService(
        IVentaNavRepository ventaRepo,
        IReservaBloqueoNavRepository bloqueoRepo,
        IStockAlmacenNavRepository stockRepo,
        IProductoNavRepository productoRepo,
        IAlmacenRepository almacenRepo,
        IAbastecimientoNavService abastecimiento,
        IStockNavService stock,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        ICurrentUserService currentUser,
        ILogger<ReservaCoberturaNavService> logger)
    {
        _ventaRepo = ventaRepo;
        _bloqueoRepo = bloqueoRepo;
        _stockRepo = stockRepo;
        _productoRepo = productoRepo;
        _almacenRepo = almacenRepo;
        _abastecimiento = abastecimiento;
        _stock = stock;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    private sealed record LineaReserva(int VentaId, DateTime Fecha, int DetalleId, int ProductoId, int Pendiente, int Bloqueado)
    {
        public int SinBloqueo => Math.Max(0, Pendiente - Bloqueado);
    }

    // Líneas con pendiente > 0 de reservas vigentes de la temporada, con lo bloqueado de cada una
    private async Task<List<LineaReserva>> ObtenerLineasAsync(int temporadaId, List<int>? productoIds, int? excluirVentaId)
    {
        var query = _ventaRepo.AsQueryable()
            .AsNoTracking()
            .Where(v => v.IsActive && !v.Anulada && v.Tipo == TipoVentaNav.Reserva && v.TemporadaId == temporadaId
                && (excluirVentaId == null || v.Id != excluirVentaId))
            .SelectMany(v => v.Detalles
                .Where(d => d.Unidades > d.UnidadesEntregadas && (productoIds == null || productoIds.Contains(d.ProductoId)))
                .Select(d => new { VentaId = v.Id, v.Fecha, DetalleId = d.Id, d.ProductoId, Pendiente = d.Unidades - d.UnidadesEntregadas }));
        var lineas = await query.ToListAsync();
        if (lineas.Count == 0) return new List<LineaReserva>();

        var detalleIds = lineas.Select(l => l.DetalleId).ToList();
        var bloqueados = await _bloqueoRepo.AsQueryable()
            .AsNoTracking()
            .Where(b => b.IsActive && detalleIds.Contains(b.VentaDetalleId))
            .GroupBy(b => b.VentaDetalleId)
            .Select(g => new { DetalleId = g.Key, Total = g.Sum(x => x.Unidades) })
            .ToDictionaryAsync(x => x.DetalleId, x => x.Total);

        return lineas
            .Select(l => new LineaReserva(l.VentaId, l.Fecha, l.DetalleId, l.ProductoId, l.Pendiente, bloqueados.GetValueOrDefault(l.DetalleId)))
            .ToList();
    }

    // Misma lógica que Faltantes: Σ de las filas (proveedor, código, producto) con faltante > 0
    private async Task<Dictionary<int, int>> ObtenerPorLlegarProductoAsync(int temporadaId, IReadOnlyCollection<int>? productoIds)
    {
        var faltantes = await _abastecimiento.ObtenerFaltantesAsync(new AbastecimientoNavFiltroDto { TemporadaId = temporadaId });
        return faltantes
            .Where(f => f.Faltante > 0 && (productoIds == null || productoIds.Contains(f.ProductoId)))
            .GroupBy(f => f.ProductoId)
            .ToDictionary(g => g.Key, g => g.Sum(f => f.Faltante));
    }

    // Σ stock de la temporada por producto (todos los almacenes con StockAlmacen). Lo cargado a ruta ya salió del almacén y no cuenta.
    private async Task<Dictionary<int, int>> ObtenerStockProductoAsync(int temporadaId, List<int>? productoIds)
        => await _stockRepo.AsQueryable()
            .AsNoTracking()
            .Where(s => s.TemporadaId == temporadaId && (productoIds == null || productoIds.Contains(s.ProductoId)))
            .GroupBy(s => s.ProductoId)
            .Select(g => new { ProductoId = g.Key, Total = g.Sum(x => x.Cantidad) })
            .ToDictionaryAsync(x => x.ProductoId, x => x.Total);

    // Cobertura global (2026-11-08): Libre = Stock + PorLlegar − Reservado (Σ pendiente de entregar, bloqueado o no)
    private sealed record ResumenProducto(int Stock, int PorLlegar, int Reservado, int SinBloqueo)
    {
        public int Libre => Stock + PorLlegar - Reservado;
        // Legado (regla anterior): por llegar − Σ partes sin bloqueo
        public int PorLlegarLibre => PorLlegar - SinBloqueo;
    }

    private async Task<Dictionary<int, ResumenProducto>> ObtenerResumenAsync(
        int temporadaId, List<int>? ids, List<LineaReserva> lineas)
    {
        var reservado = lineas.GroupBy(l => l.ProductoId)
            .ToDictionary(g => g.Key, g => (Pendiente: g.Sum(l => l.Pendiente), SinBloqueo: g.Sum(l => l.SinBloqueo)));
        var stocks = await ObtenerStockProductoAsync(temporadaId, ids);
        var porLlegar = await ObtenerPorLlegarProductoAsync(temporadaId, ids);

        var resultado = new Dictionary<int, ResumenProducto>();
        foreach (var id in stocks.Keys.Concat(porLlegar.Keys).Concat(reservado.Keys).Distinct())
        {
            var r = reservado.GetValueOrDefault(id);
            resultado[id] = new ResumenProducto(stocks.GetValueOrDefault(id), porLlegar.GetValueOrDefault(id), r.Pendiente, r.SinBloqueo);
        }
        return resultado;
    }

    public async Task<Dictionary<int, (int PorLlegar, int PorLlegarLibre, int Reservado, int Libre)>> ObtenerPorLlegarAsync(
        int temporadaId, IEnumerable<int>? productoIds = null, int? excluirVentaId = null)
    {
        var ids = productoIds?.Distinct().ToList();
        var lineas = await ObtenerLineasAsync(temporadaId, ids, excluirVentaId);
        var resumen = await ObtenerResumenAsync(temporadaId, ids, lineas);
        return resumen.ToDictionary(kv => kv.Key, kv => (kv.Value.PorLlegar, kv.Value.PorLlegarLibre, kv.Value.Reservado, kv.Value.Libre));
    }

    public async Task ValidarCoberturaAsync(int temporadaId, IReadOnlyList<(int ProductoId, string ProductoNombre, int Pendiente)> lineas, int? excluirVentaId = null)
    {
        var aValidar = lineas.Where(l => l.Pendiente > 0).ToList();
        if (aValidar.Count == 0) return;

        var ids = aValidar.Select(l => l.ProductoId).Distinct().ToList();
        var lineasReserva = await ObtenerLineasAsync(temporadaId, ids, excluirVentaId);
        var resumen = await ObtenerResumenAsync(temporadaId, ids, lineasReserva);
        foreach (var l in aValidar)
        {
            var r = resumen.GetValueOrDefault(l.ProductoId) ?? new ResumenProducto(0, 0, 0, 0);
            if (l.Pendiente > r.Libre)
                throw new ExcepcionDominio(
                    $"No alcanza para reservar {l.Pendiente} u de {l.ProductoNombre}: stock en almacenes {r.Stock} + por llegar {r.PorLlegar} − reservado pendiente {r.Reservado} = libre {r.Libre}",
                    409, "SIN_COBERTURA");
        }
    }

    public async Task ValidarLibreParaSalidaAsync(int temporadaId, IReadOnlyList<(int ProductoId, string ProductoNombre, int Unidades)> lineas,
        string accion, int? excluirVentaId = null)
    {
        var porProducto = lineas
            .Where(l => l.Unidades > 0)
            .GroupBy(l => l.ProductoId)
            .Select(g => (ProductoId: g.Key, ProductoNombre: g.First().ProductoNombre, Unidades: g.Sum(x => x.Unidades)))
            .ToList();
        if (porProducto.Count == 0) return;

        // Sin reservas pendientes de esos productos no hay nada comprometido: el stock del almacén lo valida la salida misma
        var ids = porProducto.Select(l => l.ProductoId).ToList();
        var lineasReserva = await ObtenerLineasAsync(temporadaId, ids, excluirVentaId);
        if (lineasReserva.Count == 0) return;

        var resumen = await ObtenerResumenAsync(temporadaId, ids, lineasReserva);
        foreach (var l in porProducto)
        {
            if (!resumen.TryGetValue(l.ProductoId, out var r) || r.Reservado <= 0) continue;
            if (l.Unidades > r.Libre)
                throw new ExcepcionDominio(
                    $"No se puede {accion} {l.Unidades} u de {l.ProductoNombre}: hay {r.Reservado} u reservadas pendientes de entregar y solo quedan {Math.Max(0, r.Libre)} libres (stock + por llegar)",
                    409, "RESERVAS_COMPROMETIDAS");
        }
    }

    public async Task<List<ReservaCoberturaNavDto>> ObtenerCoberturaAsync(ReservaCoberturaNavFiltroDto filtro)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(filtro.TemporadaId);

            var lineas = await ObtenerLineasAsync(temporadaId, null, null);
            if (lineas.Count == 0) return new List<ReservaCoberturaNavDto>();

            var porProducto = lineas
                .GroupBy(l => l.ProductoId)
                .Select(g => new
                {
                    ProductoId = g.Key,
                    Reservado = g.Sum(l => l.Pendiente),
                    Bloqueado = g.Sum(l => l.Pendiente - l.SinBloqueo),
                    SinBloqueo = g.Sum(l => l.SinBloqueo)
                })
                .ToList();
            var ids = porProducto.Select(r => r.ProductoId).ToList();

            var stocks = await _stockRepo.AsQueryable()
                .AsNoTracking()
                .Where(s => s.TemporadaId == temporadaId && ids.Contains(s.ProductoId))
                .GroupBy(s => s.ProductoId)
                .Select(g => new { ProductoId = g.Key, Total = g.Sum(x => x.Cantidad) })
                .ToDictionaryAsync(x => x.ProductoId, x => x.Total);

            var porLlegar = await ObtenerPorLlegarProductoAsync(temporadaId, ids);

            var productos = await _productoRepo.AsQueryable()
                .AsNoTracking()
                .Where(p => ids.Contains(p.Id))
                .Select(p => new { p.Id, Nombre = p.Nombre ?? p.Descripcion, p.UnidadesPorEmpaque, p.NombreEmpaque, p.Color })
                .ToDictionaryAsync(p => p.Id);

            // Bloqueos vigentes por almacén (detalle del tooltip)
            var detalleIds = lineas.Select(l => l.DetalleId).ToList();
            var bloqueosAlmacen = await _bloqueoRepo.AsQueryable()
                .AsNoTracking()
                .Where(b => b.IsActive && detalleIds.Contains(b.VentaDetalleId))
                .GroupBy(b => new { b.ProductoId, b.AlmacenId })
                .Select(g => new { g.Key.ProductoId, g.Key.AlmacenId, Total = g.Sum(x => x.Unidades) })
                .ToListAsync();
            var almacenIds = bloqueosAlmacen.Select(b => b.AlmacenId).Distinct().ToList();
            var nombresAlmacen = almacenIds.Count == 0
                ? new Dictionary<int, string>()
                : await _almacenRepo.AsQueryable().AsNoTracking()
                    .Where(a => almacenIds.Contains(a.Id))
                    .ToDictionaryAsync(a => a.Id, a => a.Nombre);

            return porProducto
                .Select(r =>
                {
                    productos.TryGetValue(r.ProductoId, out var p);
                    var llegar = porLlegar.GetValueOrDefault(r.ProductoId);
                    var stock = stocks.GetValueOrDefault(r.ProductoId);
                    // Cobertura global (2026-11-08): stock + por llegar − reservado pendiente (bloqueado o no)
                    var libre = stock + llegar - r.Reservado;
                    return new ReservaCoberturaNavDto
                    {
                        ProductoId = r.ProductoId,
                        ProductoNombreMostrar = p?.Nombre ?? string.Empty,
                        UnidadesPorEmpaque = p?.UnidadesPorEmpaque,
                        NombreEmpaque = p?.NombreEmpaque,
                        Color = p?.Color,
                        ReservadoPendiente = r.Reservado,
                        Bloqueado = r.Bloqueado,
                        SinBloqueo = r.SinBloqueo,
                        StockTotal = stock,
                        PorLlegar = llegar,
                        PorLlegarLibre = llegar - r.SinBloqueo,
                        Libre = libre,
                        SinCobertura = Math.Max(0, -libre),
                        Bloqueos = bloqueosAlmacen
                            .Where(b => b.ProductoId == r.ProductoId)
                            .OrderBy(b => b.AlmacenId)
                            .Select(b => new ReservaCoberturaAlmacenNavDto
                            {
                                AlmacenId = b.AlmacenId,
                                AlmacenNombre = nombresAlmacen.TryGetValue(b.AlmacenId, out var n) ? n : string.Empty,
                                Unidades = b.Total
                            }).ToList()
                    };
                })
                .OrderByDescending(x => x.SinCobertura)
                .ThenBy(x => x.ProductoNombreMostrar)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cobertura de reservas");
            throw;
        }
    }

    public async Task BloquearPorRecepcionAsync(int temporadaId, int recepcionId, IEnumerable<(int AlmacenId, int ProductoId, int Unidades)> recibido)
    {
        var porProducto = recibido
            .Where(r => r.Unidades > 0)
            .GroupBy(r => r.ProductoId)
            .ToList();
        if (porProducto.Count == 0) return;

        var productoIds = porProducto.Select(g => g.Key).ToList();
        var lineas = await ObtenerLineasAsync(temporadaId, productoIds, null);
        if (lineas.Count == 0) return;

                foreach (var grupo in porProducto)
        {
            // De la reserva más antigua a la más nueva, con su parte sin bloqueo todavía por cubrir
            var pendientes = lineas
                .Where(l => l.ProductoId == grupo.Key && l.SinBloqueo > 0)
                .OrderBy(l => l.Fecha).ThenBy(l => l.VentaId)
                .Select(l => (Linea: l, Restante: l.SinBloqueo))
                .ToList();
            if (pendientes.Count == 0) continue;

            foreach (var almacen in grupo.GroupBy(r => r.AlmacenId).Select(g => (AlmacenId: g.Key, Unidades: g.Sum(x => x.Unidades))).OrderBy(x => x.AlmacenId))
            {
                // Tope de seguridad: lo recibido, sin pasar del stock libre del almacén (por si el stock previo ya estaba comprometido)
                var libre = Math.Max(0, await _stock.ObtenerDisponibleAsync(almacen.AlmacenId, grupo.Key)
                    - await _stock.ObtenerBloqueadoAsync(almacen.AlmacenId, grupo.Key));
                var disponible = Math.Min(almacen.Unidades, libre);

                for (var i = 0; i < pendientes.Count && disponible > 0; i++)
                {
                    var (linea, restante) = pendientes[i];
                    if (restante <= 0) continue;
                    var toma = Math.Min(restante, disponible);

                    await _bloqueoRepo.CrearAsync(new ReservaBloqueo
                    {
                        VentaId = linea.VentaId,
                        VentaDetalleId = linea.DetalleId,
                        ProductoId = linea.ProductoId,
                        AlmacenId = almacen.AlmacenId,
                        Unidades = toma,
                        RecepcionId = recepcionId
                    });
                    pendientes[i] = (linea, restante - toma);
                    disponible -= toma;
                }
            }
        }
    }

    public async Task QuitarBloqueosDeRecepcionAsync(int recepcionId)
    {
        var bloqueos = await _bloqueoRepo.AsQueryable()
            .Where(b => b.IsActive && b.RecepcionId == recepcionId)
            .ToListAsync();
        if (bloqueos.Count == 0) return;

        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        var ahora = DateTime.UtcNow;
        foreach (var b in bloqueos)
            ReservaBloqueoHelper.Desactivar(b, usuarioId, ahora);
        await _uow.SaveChangesAsync();
    }
}
