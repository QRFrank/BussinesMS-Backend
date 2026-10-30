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

// Cobertura de reservas (replanteo): por línea sinBloqueo = pendiente − Σ bloqueos vigentes; por producto
// porLlegarLibre = porLlegar (misma fuente que Faltantes) − Σ sinBloqueo de las reservas vigentes.
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

    public async Task<Dictionary<int, (int PorLlegar, int PorLlegarLibre)>> ObtenerPorLlegarAsync(
        int temporadaId, IEnumerable<int>? productoIds = null, int? excluirVentaId = null)
    {
        var ids = productoIds?.Distinct().ToList();
        var lineas = await ObtenerLineasAsync(temporadaId, ids, excluirVentaId);
        var sinBloqueo = lineas.GroupBy(l => l.ProductoId).ToDictionary(g => g.Key, g => g.Sum(l => l.SinBloqueo));
        var porLlegar = await ObtenerPorLlegarProductoAsync(temporadaId, ids);

        var resultado = new Dictionary<int, (int, int)>();
        foreach (var id in porLlegar.Keys.Concat(sinBloqueo.Keys).Distinct())
        {
            var llegar = porLlegar.GetValueOrDefault(id);
            resultado[id] = (llegar, llegar - sinBloqueo.GetValueOrDefault(id));
        }
        return resultado;
    }

    public async Task ValidarCoberturaAsync(int temporadaId, IReadOnlyList<(int ProductoId, string ProductoNombre, int SinBloqueo)> lineas, int? excluirVentaId = null)
    {
        var aValidar = lineas.Where(l => l.SinBloqueo > 0).ToList();
        if (aValidar.Count == 0) return;

        var ids = aValidar.Select(l => l.ProductoId).ToList();
        var libres = await ObtenerPorLlegarAsync(temporadaId, ids, excluirVentaId);
        foreach (var l in aValidar)
        {
            var libre = libres.TryGetValue(l.ProductoId, out var v) ? v.PorLlegarLibre : 0;
            if (l.SinBloqueo > libre)
            {
                var sinCubrir = l.SinBloqueo - Math.Max(0, libre);
                throw new ExcepcionDominio(
                    $"No hay pedido pendiente que cubra {sinCubrir} u de {l.ProductoNombre}; bloqueá stock o registrá una compra",
                    409, "SIN_COBERTURA_PEDIDO");
            }
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
                    var libre = llegar - r.SinBloqueo;
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
                        StockTotal = stocks.GetValueOrDefault(r.ProductoId),
                        PorLlegar = llegar,
                        PorLlegarLibre = libre,
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
