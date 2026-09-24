using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.Helpers;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Dominio.Enums;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Sistema;

public class CompraService : ICompraService
{
    private readonly ICompraRepository _repo;
    private readonly IPagoCompraRepository _pagoRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<CompraService> _logger;
    private readonly IInventarioLoteRepository _loteRepo;
    private readonly IInventarioLoteAlmacenRepository _loteAlmacenRepo;
    private readonly IMovimientoInventarioRepository _movimientoRepo;
    private readonly IProductoVarianteRepository _varianteRepo;
    private readonly ISistemaUnitOfWork _uow;

    public CompraService(
        ICompraRepository repo,
        IPagoCompraRepository pagoRepo,
        IMapper mapper,
        ILogger<CompraService> logger,
        IInventarioLoteRepository loteRepo,
        IInventarioLoteAlmacenRepository loteAlmacenRepo,
        IMovimientoInventarioRepository movimientoRepo,
        IProductoVarianteRepository varianteRepo,
        ISistemaUnitOfWork uow)
    {
        _repo = repo;
        _pagoRepo = pagoRepo;
        _mapper = mapper;
        _logger = logger;
        _loteRepo = loteRepo;
        _loteAlmacenRepo = loteAlmacenRepo;
        _movimientoRepo = movimientoRepo;
        _varianteRepo = varianteRepo;
        _uow = uow;
    }

    public async Task<PagedResultDto<CompraListDto>> ObtenerTodosAsync(CompraFiltroDto query)
    {
        try
        {
            var baseQuery = _repo.AsQueryable().Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    x.Observacion != null && x.Observacion.ToLower().Contains(f));
            }

            if (query.ProveedorId.HasValue)
                baseQuery = baseQuery.Where(x => x.ProveedorId == query.ProveedorId.Value);

            if (query.AlmacenId.HasValue)
                baseQuery = baseQuery.Where(x => x.AlmacenId == query.AlmacenId.Value);

            if (query.EstadoPago.HasValue)
                baseQuery = baseQuery.Where(x => x.EstadoPago == (EstadoPago)query.EstadoPago.Value);

            if (query.EstaLiquidada.HasValue)
                baseQuery = baseQuery.Where(x => x.EstaLiquidada == query.EstaLiquidada.Value);

            if (query.FechaDesde.HasValue)
            {
                var (inicioUtc, _) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(query.FechaDesde.Value));
                baseQuery = baseQuery.Where(x => x.FechaCompra >= inicioUtc);
            }

            if (query.FechaHasta.HasValue)
            {
                var (_, finUtc) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(query.FechaHasta.Value));
                baseQuery = baseQuery.Where(x => x.FechaCompra < finUtc);
            }

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query);
            var entidades = await filteredQuery
                .Include(x => x.Proveedor)
                .Include(x => x.Detalles)
                .Include(x => x.Pagos)
                .ToListAsync();

            var items = entidades.Select(e => new CompraListDto
            {
                Id = e.Id,
                ProveedorId = e.ProveedorId,
                ProveedorNombre = e.Proveedor?.Nombre,
                UsuarioId = e.UsuarioId,
                AlmacenId = e.AlmacenId,
                FechaCompra = BoliviaTimeZone.ToLocal(e.FechaCompra),
                TotalCompra = e.TotalCompra,
                EstadoPago = e.EstadoPago,
                NumeroFactura = e.NumeroFactura,
                EstaLiquidada = e.EstaLiquidada,
                Observacion = e.Observacion,
                IsActive = e.IsActive,
                CreatedAt = BoliviaTimeZone.ToLocal(e.CreatedAt),
                CantidadDetalles = e.Detalles.Count,
                CantidadPagos = e.Pagos.Count
            }).ToList();

            return new PagedResultDto<CompraListDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener compras");
            throw;
        }
    }

    public async Task<CompraDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerConDetallesAsync(id);
            if (entidad == null || !entidad.IsActive) return null;

            var compraDto = _mapper.Map<CompraDto>(entidad);

            foreach (var detalleDto in compraDto.Detalles)
            {
                var lote = await _loteRepo.AsQueryable()
                    .FirstOrDefaultAsync(l => l.CompraDetalleId == detalleDto.Id);

                if (lote != null)
                {
                    var almacenesDelLote = await _loteAlmacenRepo.AsQueryable()
                        .Include(la => la.Lote)
                        .Where(la => la.LoteId == lote.Id)
                        .Select(la => new CompraDetalleAlmacenDto
                        {
                            InventarioLoteAlmacenId = la.Id,
                            AlmacenId = la.AlmacenId,
                            CantidadUnidades = la.StockDisponible,
                            StockDisponible = la.StockDisponible
                        }).ToListAsync();

                    detalleDto.Almacenes = almacenesDelLote;
                }
            }

            return compraDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener compra {Id}", id);
            throw;
        }
    }

    public async Task<CompraDto> CrearAsync(CrearCompraDto dto)
    {
        try
        {
            if (dto.Detalles == null || !dto.Detalles.Any())
                throw new ValidacionException("La compra debe tener al menos un detalle");

            foreach (var d in dto.Detalles)
            {
                if (d.Almacenes == null || !d.Almacenes.Any())
                    throw new ValidacionException($"El detalle de variante {d.VarianteId} debe tener al menos un almacén");

                if (d.Almacenes.Any(a => a.CantidadUnidades <= 0))
                    throw new ValidacionException($"Todas las cantidades por almacén deben ser mayores a 0");

                if (d.PrecioVentaUnitario <= 0)
                    throw new ValidacionException("El precio de venta unitario debe ser mayor a 0");
                if (d.PrecioVentaMayor <= 0)
                    throw new ValidacionException("El precio de venta mayoreo debe ser mayor a 0");
            }

            await _uow.BeginTransactionAsync();
            try
            {
                var entidad = new Compra
                {
                    ProveedorId = dto.ProveedorId,
                    AlmacenId = dto.AlmacenId,
                    EstadoPago = dto.EstadoPago,
                    NumeroFactura = dto.NumeroFactura,
                    Observacion = dto.Observacion,
                    FechaCompra = DateTime.UtcNow
                };

                entidad.TotalCompra = dto.Detalles.Sum(d =>
                    d.Almacenes.Sum(a => a.CantidadUnidades) * d.CostoUnitario);

                var pares = dto.Detalles.Select(detalleDto =>
                {
                    var cantidadTotal = detalleDto.Almacenes.Sum(a => a.CantidadUnidades);
                    var detalle = new CompraDetalle
                    {
                        VarianteId = detalleDto.VarianteId,
                        CantidadUnidades = cantidadTotal,
                        CostoUnitario = detalleDto.CostoUnitario,
                        Subtotal = cantidadTotal * detalleDto.CostoUnitario,
                        FechaVencimiento = detalleDto.FechaVencimiento
                    };
                    return (detalle, detalleDto);
                }).ToList();

                foreach (var (detalle, _) in pares)
                    entidad.Detalles.Add(detalle);

                await _repo.CrearSinGuardarAsync(entidad);
                await _uow.SaveChangesAsync();

                var lotes = new List<(InventarioLote lote, CrearCompraDetalleDto detalleDto)>();
                var fechaLocal = BoliviaTimeZone.Now();
                foreach (var (detalle, detalleDto) in pares)
                {
                    var codigoLote = await GenerarCodigoLoteUnicoAsync(detalle.VarianteId, fechaLocal);

                    var lote = new InventarioLote
                    {
                        VarianteId = detalle.VarianteId,
                        CompraDetalleId = detalle.Id,
                        CodigoLote = codigoLote,
                        CostoCompraUnitario = detalle.CostoUnitario,
                        StockInicial = detalle.CantidadUnidades,
                        CantidadVendida = 0,
                        CantidadVencida = 0,
                        EstadoLote = EstadoLote.Activo,
                        FechaVencimiento = detalle.FechaVencimiento
                    };
                    await _loteRepo.CrearSinGuardarAsync(lote);
                    lotes.Add((lote, detalleDto));
                }

                await _uow.SaveChangesAsync();

                var todosLoteAlmacenes = new List<InventarioLoteAlmacen>();
                foreach (var (lote, detalleDto) in lotes)
                {
                    foreach (var alm in detalleDto.Almacenes)
                    {
                        var loteAlmacen = new InventarioLoteAlmacen
                        {
                            LoteId = lote.Id,
                            VarianteId = lote.VarianteId,
                            AlmacenId = alm.AlmacenId,
                            StockDisponible = alm.CantidadUnidades
                        };
                        await _loteAlmacenRepo.CrearSinGuardarAsync(loteAlmacen);
                        todosLoteAlmacenes.Add(loteAlmacen);
                    }
                }

                await _uow.SaveChangesAsync();

                foreach (var loteAlmacen in todosLoteAlmacenes)
                {
                    var movimiento = new MovimientoInventario
                    {
                        LoteAlmacenId = loteAlmacen.Id,
                        VarianteId = loteAlmacen.VarianteId,
                        AlmacenOrigenId = null,
                        AlmacenDestinoId = loteAlmacen.AlmacenId,
                        TipoMovimiento = TipoMovimiento.EntradaCompra,
                        CantidadUnidades = loteAlmacen.StockDisponible,
                        SaldoResultante = loteAlmacen.StockDisponible,
                        ReferenciaId = loteAlmacen.Lote?.CompraDetalleId,
                        Observacion = "Entrada por compra"
                    };
                    await _movimientoRepo.CrearSinGuardarAsync(movimiento);
                }

                await _uow.SaveChangesAsync();

                foreach (var (detalle, detalleDto) in pares)
                {
                    if (detalleDto.ActualizarPrecioVenta)
                    {
                        var variante = await _varianteRepo.ObtenerPorIdAsync(detalle.VarianteId);
                        if (variante != null)
                        {
                            variante.PrecioVentaUnitario = detalleDto.PrecioVentaUnitario;
                            variante.PrecioVentaMayoreo = detalleDto.PrecioVentaMayor;
                            variante.PrecioCompra = detalleDto.CostoUnitario;
                            await _varianteRepo.ActualizarAsync(variante);
                        }
                    }
                }
                await _uow.SaveChangesAsync();

                switch (dto.EstadoPago)
                {
                    case EstadoPago.Contado:
                        if (dto.PagadoPorUsuarioId <= 0)
                            throw new ValidacionException("El campo PagadoPorUsuarioId es requerido para pago al contado");

                        var pagoContado = new PagoCompra
                        {
                            CompraId = entidad.Id,
                            Monto = entidad.TotalCompra,
                            FechaPago = DateTime.UtcNow,
                            SesionCajaId = null,
                            PagadoPorUsuarioId = dto.PagadoPorUsuarioId,
                            Observacion = "Pago completo al contado"
                        };
                        await _pagoRepo.CrearSinGuardarAsync(pagoContado);
                        await _uow.SaveChangesAsync();

                        entidad.EstaLiquidada = true;
                        _repo.ActualizarAsync(entidad).Wait();
                        await _uow.SaveChangesAsync();
                        break;

                    case EstadoPago.Credito:
                        entidad.EstaLiquidada = false;
                        break;

                    case EstadoPago.ParcialmentePagado:
                        if (dto.MontoParcial == null || dto.MontoParcial <= 0)
                            throw new ValidacionException("El monto parcial es requerido y debe ser mayor a 0 para estado ParcialmentePagado");

                        if (dto.MontoParcial >= entidad.TotalCompra)
                            throw new ValidacionException("El monto parcial debe ser menor al total de la compra");

                        if (dto.PagadoPorUsuarioId <= 0)
                            throw new ValidacionException("El campo PagadoPorUsuarioId es requerido para pago parcial");

                        var pagoParcial = new PagoCompra
                        {
                            CompraId = entidad.Id,
                            Monto = dto.MontoParcial.Value,
                            FechaPago = DateTime.UtcNow,
                            SesionCajaId = null,
                            PagadoPorUsuarioId = dto.PagadoPorUsuarioId,
                            Observacion = "Pago parcial al registrar compra"
                        };
                        await _pagoRepo.CrearSinGuardarAsync(pagoParcial);
                        await _uow.SaveChangesAsync();

                        entidad.EstaLiquidada = false;
                        break;
                }

                await _uow.CommitAsync();

                var compraDto = _mapper.Map<CompraDto>(entidad);
                foreach (var detalleDto in compraDto.Detalles)
                {
                    var lote = lotes.FirstOrDefault(l => l.lote.CompraDetalleId == detalleDto.Id).lote;
                    if (lote != null)
                    {
                        var almacenesDelLote = todosLoteAlmacenes
                            .Where(la => la.LoteId == lote.Id)
                            .Select(la => new CompraDetalleAlmacenDto
                            {
                                InventarioLoteAlmacenId = la.Id,
                                AlmacenId = la.AlmacenId,
                                CantidadUnidades = la.StockDisponible,
                                StockDisponible = la.StockDisponible
                            }).ToList();
                        detalleDto.Almacenes = almacenesDelLote;
                    }
                }

                _logger.LogInformation("Compra creada: {Id} - Total: {Total} - Lotes: {Cantidad} - Estado: {Estado}",
                    entidad.Id, entidad.TotalCompra, pares.Count, dto.EstadoPago);

                return compraDto;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear compra");
            throw;
        }
    }

    public async Task<CompraDto> ActualizarAsync(ActualizarCompraDto dto)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(dto.Id);
            ValidacionEntidad.VerificarActivo(existente, "Compra");

            existente!.ProveedorId = dto.ProveedorId;
            existente.AlmacenId = dto.AlmacenId;
            existente.EstadoPago = dto.EstadoPago;
            existente.NumeroFactura = dto.NumeroFactura;
            existente.Observacion = dto.Observacion;
            existente.IsActive = dto.IsActive;

            var actualizada = await _repo.ActualizarAsync(existente);

            _logger.LogInformation("Compra actualizada: {Id}", actualizada.Id);

            return _mapper.Map<CompraDto>(actualizada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar compra {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            ValidacionEntidad.VerificarActivo(existente, "Compra");

            await _repo.EliminarAsync(id);
            _logger.LogInformation("Compra eliminada: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar compra {Id}", id);
            throw;
        }
    }

    public async Task<PagoCompraDto> AgregarPagoAsync(int compraId, CrearPagoCompraDto dto)
    {
        try
        {
            var compra = await _repo.ObtenerConDetallesAsync(compraId);
            if (compra == null || !compra.IsActive)
                throw new EntidadNoEncontradaException("Compra", compraId);

            if (compra.EstaLiquidada)
                throw new ValidacionException("La compra ya está liquidada");

            if (dto.Monto <= 0)
                throw new ValidacionException("El monto debe ser mayor a 0");

            if (dto.PagadoPorUsuarioId <= 0)
                throw new ValidacionException("El campo PagadoPorUsuarioId es requerido");

            var totalPagadoExistente = compra.Pagos
                .Where(p => p.IsActive)
                .Sum(p => p.Monto);
            var totalPagado = totalPagadoExistente + dto.Monto;

            if (totalPagado > compra.TotalCompra)
                throw new ValidacionException("El monto excede el total de la compra");

            await _uow.BeginTransactionAsync();
            try
            {
                var pago = new PagoCompra
                {
                    CompraId = compraId,
                    Monto = dto.Monto,
                    FechaPago = DateTime.UtcNow,
                    SesionCajaId = dto.SesionCajaId,
                    PagadoPorUsuarioId = dto.PagadoPorUsuarioId,
                    Observacion = dto.Observacion
                };

                await _pagoRepo.CrearSinGuardarAsync(pago);
                await _uow.SaveChangesAsync();

                if (totalPagado == compra.TotalCompra)
                {
                    compra.EstaLiquidada = true;
                    compra.EstadoPago = EstadoPago.Contado;
                }
                else
                {
                    compra.EstadoPago = EstadoPago.ParcialmentePagado;
                }

                await _repo.ActualizarAsync(compra);
                await _uow.SaveChangesAsync();

                await _uow.CommitAsync();

                _logger.LogInformation("Pago registrado para compra {CompraId}: {Monto} - Total pagado: {TotalPagado}",
                    compraId, dto.Monto, totalPagado);

                return _mapper.Map<PagoCompraDto>(pago);
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar pago a compra {CompraId}", compraId);
            throw;
        }
    }

    private async Task<string> GenerarCodigoLoteUnicoAsync(int varianteId, DateTime fechaLocal)
    {
        var variante = await _varianteRepo.ObtenerConDetallesAsync(varianteId)
            ?? throw new ValidacionException($"Variante {varianteId} no encontrada");

        var nombreProducto = variante.Producto?.Nombre ?? string.Empty;

        var textoPresentacion = variante.Tamanio?.Nombre ?? "UN";

        var codigoBase = CodigoLoteGenerator.Generar(nombreProducto, textoPresentacion, fechaLocal);

        var codigoLote = codigoBase;
        var n = 2;
        while (await _loteRepo.AsQueryable().AnyAsync(l => l.CodigoLote == codigoLote && l.IsActive))
        {
            codigoLote = $"{codigoBase}-{n}";
            n++;
        }

        return codigoLote;
    }
}
