using AutoMapper;
using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Dominio.Enums;
using BussinesMS.Dominio.Excepciones;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Sistema;

public class TrasladoService : ITrasladoService
{
    private readonly IInventarioLoteAlmacenRepository _loteAlmacenRepo;
    private readonly IMovimientoInventarioRepository _movimientoRepo;
    private readonly ISistemaUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<TrasladoService> _logger;

    public TrasladoService(
        IInventarioLoteAlmacenRepository loteAlmacenRepo,
        IMovimientoInventarioRepository movimientoRepo,
        ISistemaUnitOfWork uow,
        IMapper mapper,
        ILogger<TrasladoService> logger)
    {
        _loteAlmacenRepo = loteAlmacenRepo;
        _movimientoRepo = movimientoRepo;
        _uow = uow;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<MovimientoInventarioDto> CrearPorLoteAsync(CrearTrasladoPorLoteDto dto)
    {
        try
        {
            if (dto.CantidadUnidades <= 0)
                throw new ValidacionException("La cantidad de unidades debe ser mayor a 0");

            if (dto.AlmacenOrigenId == dto.AlmacenDestinoId)
                throw new ValidacionException("El almacén de origen y destino no pueden ser el mismo");

            var loteOrigen = await _loteAlmacenRepo.ObtenerPorLoteYAlmacenAsync(dto.LoteId, dto.AlmacenOrigenId);
            if (loteOrigen == null)
                throw new EntidadNoEncontradaException("InventarioLoteAlmacen", dto.LoteId);

            if (dto.CantidadUnidades > loteOrigen.StockDisponible)
                throw new ValidacionException("La cantidad a trasladar excede el stock disponible del lote origen");

            await _uow.BeginTransactionAsync();
            try
            {
                loteOrigen.StockDisponible -= dto.CantidadUnidades;
                await _loteAlmacenRepo.ActualizarAsync(loteOrigen);

                var destinoExistente = await _loteAlmacenRepo.ObtenerPorLoteYAlmacenAsync(
                    dto.LoteId, dto.AlmacenDestinoId);

                InventarioLoteAlmacen loteDestino;
                if (destinoExistente != null)
                {
                    destinoExistente.StockDisponible += dto.CantidadUnidades;
                    await _loteAlmacenRepo.ActualizarAsync(destinoExistente);
                    loteDestino = destinoExistente;
                }
                else
                {
                    loteDestino = new InventarioLoteAlmacen
                    {
                        LoteId = dto.LoteId,
                        VarianteId = loteOrigen.VarianteId,
                        AlmacenId = dto.AlmacenDestinoId,
                        StockDisponible = dto.CantidadUnidades
                    };
                    loteDestino = await _loteAlmacenRepo.CrearSinGuardarAsync(loteDestino);
                }

                var movimiento = new MovimientoInventario
                {
                    LoteAlmacenId = loteOrigen.Id,
                    VarianteId = loteOrigen.VarianteId,
                    AlmacenOrigenId = dto.AlmacenOrigenId,
                    AlmacenDestinoId = dto.AlmacenDestinoId,
                    TipoMovimiento = TipoMovimiento.Traslado,
                    CantidadUnidades = dto.CantidadUnidades,
                    SaldoResultante = loteOrigen.StockDisponible,
                    Observacion = $"Traslado de almacén {dto.AlmacenOrigenId} a {dto.AlmacenDestinoId}"
                };
                await _movimientoRepo.CrearSinGuardarAsync(movimiento);

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                _logger.LogInformation(
                    "Traslado por lote: Lote={LoteId}, De={Origen} A={Destino}, Cantidad={Cantidad}",
                    dto.LoteId, dto.AlmacenOrigenId, dto.AlmacenDestinoId, dto.CantidadUnidades);

                return _mapper.Map<MovimientoInventarioDto>(movimiento);
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear traslado por lote");
            throw;
        }
    }
}
