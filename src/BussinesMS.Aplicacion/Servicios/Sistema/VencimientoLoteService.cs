using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Dominio.Enums;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Sistema;

public class VencimientoLoteService : IVencimientoLoteService
{
    private readonly IInventarioLoteAlmacenRepository _loteAlmacenRepo;
    private readonly IInventarioLoteRepository _loteRepo;
    private readonly IMovimientoInventarioRepository _movimientoRepo;
    private readonly ILogger<VencimientoLoteService> _logger;

    public VencimientoLoteService(
        IInventarioLoteAlmacenRepository loteAlmacenRepo,
        IInventarioLoteRepository loteRepo,
        IMovimientoInventarioRepository movimientoRepo,
        ILogger<VencimientoLoteService> logger)
    {
        _loteAlmacenRepo = loteAlmacenRepo;
        _loteRepo = loteRepo;
        _movimientoRepo = movimientoRepo;
        _logger = logger;
    }

    public Task<List<InventarioLoteAlmacen>> ObtenerLotesVencidosAsync(DateTime corte, int? almacenId = null, List<int>? varianteIds = null)
        => _loteAlmacenRepo.ObtenerLotesVencidosAsync(corte, almacenId, varianteIds);

    public async Task<int> MarcarComoVencidosAsync(IEnumerable<int> loteIds)
    {
        var ids = loteIds.Distinct().ToList();
        if (!ids.Any()) return 0;

        var procesados = 0;
        foreach (var loteId in ids)
        {
            var lote = await _loteRepo.ObtenerPorIdAsync(loteId);
            if (lote == null || !lote.IsActive
                || lote.EstadoLote == EstadoLote.Vencido
                || lote.EstadoLote == EstadoLote.Baja
                || lote.EstadoLote == EstadoLote.Devuelto)
                continue;

            var almacenes = await _loteAlmacenRepo.ObtenerPorLoteAsync(loteId);
            var cantidadTotalVencida = almacenes.Sum(a => a.StockDisponible);

            lote.CantidadVencida += cantidadTotalVencida;
            lote.EstadoLote = EstadoLote.Vencido;
            await _loteRepo.ActualizarAsync(lote);

            foreach (var loteAlmacen in almacenes)
            {
                if (loteAlmacen.StockDisponible > 0)
                {
                    var movimiento = new MovimientoInventario
                    {
                        LoteAlmacenId = loteAlmacen.Id,
                        VarianteId = lote.VarianteId,
                        AlmacenOrigenId = loteAlmacen.AlmacenId,
                        TipoMovimiento = TipoMovimiento.Vencimiento,
                        CantidadUnidades = loteAlmacen.StockDisponible,
                        SaldoResultante = 0,
                        Observacion = $"Vencimiento automático - Stock vencido: {loteAlmacen.StockDisponible}"
                    };
                    await _movimientoRepo.CrearAsync(movimiento);

                    loteAlmacen.StockDisponible = 0;
                    await _loteAlmacenRepo.ActualizarAsync(loteAlmacen);
                }
            }

            _logger.LogInformation(
                "Lote {LoteId} vencido: {Cantidad} unidades marcadas como vencidas",
                loteId, cantidadTotalVencida);

            procesados++;
        }

        return procesados;
    }

    public async Task<int> ProcesarVencimientosPendientesAsync(int? almacenId = null, List<int>? varianteIds = null)
    {
        var (inicioHoyUtc, _) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(DateTime.UtcNow));

        var vencidos = await ObtenerLotesVencidosAsync(inicioHoyUtc, almacenId, varianteIds);
        if (!vencidos.Any())
        {
            _logger.LogInformation("ProcesarVencimientosPendientesAsync: No hay lotes por vencer");
            return 0;
        }

        var ids = vencidos.Select(v => v.LoteId).Distinct();
        return await MarcarComoVencidosAsync(ids);
    }
}
