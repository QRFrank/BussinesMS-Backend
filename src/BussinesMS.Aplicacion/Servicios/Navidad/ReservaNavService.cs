using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Cobertura de reservas: reservado pendiente vs stock actual + lo que falta llegar del proveedor.
public class ReservaNavService : IReservaNavService
{
    private readonly IVentaNavRepository _ventaRepo;
    private readonly IStockAlmacenNavRepository _stockRepo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IAbastecimientoNavService _abastecimiento;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly ILogger<ReservaNavService> _logger;

    public ReservaNavService(
        IVentaNavRepository ventaRepo,
        IStockAlmacenNavRepository stockRepo,
        IProductoNavRepository productoRepo,
        IAbastecimientoNavService abastecimiento,
        ITemporadaActualService temporadaActual,
        ILogger<ReservaNavService> logger)
    {
        _ventaRepo = ventaRepo;
        _stockRepo = stockRepo;
        _productoRepo = productoRepo;
        _abastecimiento = abastecimiento;
        _temporadaActual = temporadaActual;
        _logger = logger;
    }

    public async Task<List<ReservaCoberturaNavDto>> ObtenerCoberturaAsync(ReservaCoberturaNavFiltroDto filtro)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(filtro.TemporadaId);
            return await CalcularAsync(temporadaId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cobertura de reservas");
            throw;
        }
    }

    public async Task<List<string>> ObtenerAdvertenciasAsync(int temporadaId, IEnumerable<int> productoIds)
    {
        var ids = productoIds.Distinct().ToList();
        if (ids.Count == 0) return new List<string>();

        var filas = await CalcularAsync(temporadaId, ids);
        return filas
            .Where(f => f.SobreReservado)
            .Select(f => $"El producto '{f.ProductoNombreMostrar}' queda sobre-reservado: reservado pendiente {f.ReservadoPendiente}, stock {f.StockTotal} + por llegar {f.PorLlegar}")
            .ToList();
    }

    private async Task<List<ReservaCoberturaNavDto>> CalcularAsync(int temporadaId, List<int>? productoIds)
    {
        var lineas = _ventaRepo.AsQueryable()
            .AsNoTracking()
            .Where(v => v.IsActive && !v.Anulada && v.Tipo == TipoVentaNav.Reserva && v.TemporadaId == temporadaId)
            .SelectMany(v => v.Detalles
                .Where(d => d.Unidades > d.UnidadesEntregadas)
                .Select(d => new { d.ProductoId, v.BloquearStock, Pendiente = d.Unidades - d.UnidadesEntregadas }));
        if (productoIds != null)
            lineas = lineas.Where(x => productoIds.Contains(x.ProductoId));

        var reservados = await lineas
            .GroupBy(x => x.ProductoId)
            .Select(g => new
            {
                ProductoId = g.Key,
                Reservado = g.Sum(x => x.Pendiente),
                Bloqueado = g.Sum(x => x.BloquearStock ? x.Pendiente : 0)
            })
            .ToListAsync();

        if (reservados.Count == 0) return new List<ReservaCoberturaNavDto>();

        var ids = reservados.Select(r => r.ProductoId).ToList();

        var stocks = await _stockRepo.AsQueryable()
            .AsNoTracking()
            .Where(s => s.TemporadaId == temporadaId && ids.Contains(s.ProductoId))
            .GroupBy(s => s.ProductoId)
            .Select(g => new { ProductoId = g.Key, Total = g.Sum(x => x.Cantidad) })
            .ToDictionaryAsync(x => x.ProductoId, x => x.Total);

        // Por llegar = misma lógica que Faltantes: Σ de las filas (proveedor, código, producto) con faltante > 0
        var faltantes = await _abastecimiento.ObtenerFaltantesAsync(new AbastecimientoNavFiltroDto { TemporadaId = temporadaId });
        var porLlegar = faltantes
            .Where(f => f.Faltante > 0 && ids.Contains(f.ProductoId))
            .GroupBy(f => f.ProductoId)
            .ToDictionary(g => g.Key, g => g.Sum(f => f.Faltante));

        var productos = await _productoRepo.AsQueryable()
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, Nombre = p.Nombre ?? p.Descripcion, p.UnidadesPorEmpaque, p.NombreEmpaque, p.Color })
            .ToDictionaryAsync(p => p.Id);

        return reservados
            .Select(r =>
            {
                productos.TryGetValue(r.ProductoId, out var p);
                var stock = stocks.GetValueOrDefault(r.ProductoId);
                var llegar = porLlegar.GetValueOrDefault(r.ProductoId);
                var diferencia = stock + llegar - r.Reservado;
                return new ReservaCoberturaNavDto
                {
                    ProductoId = r.ProductoId,
                    ProductoNombreMostrar = p?.Nombre ?? string.Empty,
                    UnidadesPorEmpaque = p?.UnidadesPorEmpaque,
                    NombreEmpaque = p?.NombreEmpaque,
                    Color = p?.Color,
                    ReservadoPendiente = r.Reservado,
                    Bloqueado = r.Bloqueado,
                    StockTotal = stock,
                    PorLlegar = llegar,
                    SobreReservado = diferencia < 0,
                    Diferencia = diferencia
                };
            })
            .OrderByDescending(x => x.SobreReservado)
            .ThenBy(x => x.ProductoNombreMostrar)
            .ToList();
    }
}
