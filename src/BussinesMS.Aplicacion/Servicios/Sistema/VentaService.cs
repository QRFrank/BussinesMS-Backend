using AutoMapper;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Helpers;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Dominio.Enums;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Sistema;

public class VentaService : IVentaService
{
    private readonly IVentaRepository _ventaRepo;
    private readonly ISesionCajaRepository _sesionRepo;
    private readonly IInventarioLoteAlmacenRepository _loteAlmacenRepo;
    private readonly IInventarioLoteRepository _loteRepo;
    private readonly IMovimientoInventarioRepository _movimientoRepo;
    private readonly IVencimientoLoteService _vencimientoService;
    private readonly IClienteRepository _clienteRepo;
    private readonly IProductoVarianteRepository _varianteRepo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly ISistemaUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<VentaService> _logger;

    public VentaService(
        IVentaRepository ventaRepo,
        ISesionCajaRepository sesionRepo,
        IInventarioLoteAlmacenRepository loteAlmacenRepo,
        IInventarioLoteRepository loteRepo,
        IMovimientoInventarioRepository movimientoRepo,
        IVencimientoLoteService vencimientoService,
        IClienteRepository clienteRepo,
        IProductoVarianteRepository varianteRepo,
        IAlmacenRepository almacenRepo,
        ISistemaUnitOfWork uow,
        IMapper mapper,
        ILogger<VentaService> logger)
    {
        _ventaRepo = ventaRepo;
        _sesionRepo = sesionRepo;
        _loteAlmacenRepo = loteAlmacenRepo;
        _loteRepo = loteRepo;
        _movimientoRepo = movimientoRepo;
        _vencimientoService = vencimientoService;
        _clienteRepo = clienteRepo;
        _varianteRepo = varianteRepo;
        _almacenRepo = almacenRepo;
        _uow = uow;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<VentaListDto>> ObtenerTodosAsync(VentaFiltroDto query)
    {
        try
        {
            var todos = await _ventaRepo.ObtenerTodosAsync();

            if (query.AlmacenId.HasValue)
                todos = todos.Where(x => x.AlmacenId == query.AlmacenId.Value).ToList();

            if (query.SesionCajaId.HasValue)
                todos = todos.Where(x => x.SesionCajaId == query.SesionCajaId.Value).ToList();

            if (query.MetodoPago.HasValue)
                todos = todos.Where(x => x.MetodoPago == query.MetodoPago.Value).ToList();

            if (query.FechaDesde.HasValue)
            {
                var (inicioUtc, _) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(query.FechaDesde.Value));
                todos = todos.Where(x => x.FechaVenta >= inicioUtc).ToList();
            }

            if (query.FechaHasta.HasValue)
            {
                var (_, finUtc) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(query.FechaHasta.Value));
                todos = todos.Where(x => x.FechaVenta < finUtc).ToList();
            }

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var filtro = query.Filter.ToLower();
                todos = todos.Where(x =>
                    x.MotivoDescuento != null && x.MotivoDescuento.ToLower().Contains(filtro)
                ).ToList();
            }

            var totalCount = todos.Count;
            var paginados = todos
                .Skip((query.GetPageValue() - 1) * query.GetPageSizeValue())
                .Take(query.GetPageSizeValue())
                .ToList();

            var listaDto = paginados.Select(v => new VentaListDto
            {
                Id = v.Id,
                UsuarioId = v.UsuarioId,
                AlmacenId = v.AlmacenId,
                SesionCajaId = v.SesionCajaId,
                FechaVenta = BoliviaTimeZone.ToLocal(v.FechaVenta),
                TotalBruto = v.TotalBruto,
                DescuentoTotal = v.DescuentoTotal,
                TotalNeto = v.TotalNeto,
                MetodoPago = v.MetodoPago,
                MontoEfectivo = v.MontoEfectivo,
                MontoTransferencia = v.MontoTransferencia,
                MontoRecibido = v.MontoRecibido,
                Cambio = v.Cambio,
                MotivoDescuento = v.MotivoDescuento,
                IsActive = v.IsActive,
                CreatedAt = BoliviaTimeZone.ToLocal(v.CreatedAt),
                ClienteId = v.ClienteId,
                ClienteNombre = v.Cliente?.Nombre,
                CantidadDetalles = v.Detalles?.Count ?? 0
            }).ToList();

            return new PagedResultDto<VentaListDto>
            {
                Items = listaDto,
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ventas");
            throw;
        }
    }

    public async Task<VentaDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _ventaRepo.ObtenerConDetallesAsync(id);
            if (entidad == null) return null;

            var dto = _mapper.Map<VentaDto>(entidad);
            dto.FechaVenta = BoliviaTimeZone.ToLocal(entidad.FechaVenta);
            dto.CreatedAt = BoliviaTimeZone.ToLocal(entidad.CreatedAt);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener venta {Id}", id);
            throw;
        }
    }

    public async Task<VentaDto> CrearAsync(CrearVentaDto dto)
    {
        try
        {
            if (dto.Detalles == null || !dto.Detalles.Any())
                throw new ValidacionException("La venta debe tener al menos un detalle");

            foreach (var d in dto.Detalles)
            {
                if (d.CantidadUnidades <= 0)
                    throw new ValidacionException($"La cantidad del variante {d.VarianteId} debe ser mayor a 0");

                if (d.PrecioUnitarioCobrado <= 0)
                    throw new ValidacionException($"El precio del variante {d.VarianteId} debe ser mayor a 0");
            }

            var sesion = await _sesionRepo.ObtenerPorIdAsync(dto.SesionCajaId);
            if (sesion == null)
                throw new EntidadNoEncontradaException("SesionCaja", dto.SesionCajaId);

            ValidacionEntidad.VerificarActivo(sesion, "Sesión de caja");

            if (sesion.Estado != EstadoSesionCaja.Abierta)
                throw new ValidacionException("La sesión de caja debe estar abierta para registrar ventas");

            if (dto.DescuentoTotal.HasValue && dto.DescuentoTotal > 0 && string.IsNullOrWhiteSpace(dto.MotivoDescuento))
                throw new ValidacionException("El motivo del descuento es requerido cuando hay descuento");

            var clienteId = Cliente.ClienteGenericoId;
            if (dto.ClienteId.HasValue && dto.ClienteId.Value > 0)
            {
                var cliente = await _clienteRepo.ObtenerPorIdAsync(dto.ClienteId.Value);
                if (cliente == null)
                    throw new EntidadNoEncontradaException("Cliente", dto.ClienteId.Value);

                ValidacionEntidad.VerificarActivo(cliente, "Cliente");
                clienteId = cliente.Id;
            }

            await _uow.BeginTransactionAsync();
            try
            {
                var venta = new Venta
                {
                    SesionCajaId = dto.SesionCajaId,
                    AlmacenId = sesion.AlmacenId,
                    ClienteId = clienteId,
                    MetodoPago = dto.MetodoPago,
                    DescuentoTotal = dto.DescuentoTotal ?? 0,
                    MotivoDescuento = dto.MotivoDescuento,
                    FechaVenta = DateTime.UtcNow
                };

                await _ventaRepo.CrearSinGuardarAsync(venta);
                await _uow.SaveChangesAsync();

                var totalBruto = 0m;
                var detallesCreados = new List<VentaDetalle>();

                var (inicioHoyUtc, _) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(DateTime.UtcNow));
                var variantesVenta = dto.Detalles.Select(d => d.VarianteId).Distinct().ToList();
                var lotesVencidos = await _vencimientoService.ObtenerLotesVencidosAsync(
                    inicioHoyUtc, sesion.AlmacenId, variantesVenta);
                if (lotesVencidos.Any())
                {
                    await _vencimientoService.MarcarComoVencidosAsync(
                        lotesVencidos.Select(l => l.LoteId).Distinct());
                }

                foreach (var detalleDto in dto.Detalles)
                {
                    var cantidadRestante = detalleDto.CantidadUnidades;
                    var subtotalDetalle = 0m;

                    var lotesAlmacen = await _loteAlmacenRepo.ObtenerLotesFEFOAsync(detalleDto.VarianteId, sesion.AlmacenId);

                    if (!lotesAlmacen.Any())
                        throw new ValidacionException($"No hay stock disponible para el variante {detalleDto.VarianteId}");

                    var stockTotal = lotesAlmacen.Sum(la => la.StockDisponible);
                    if (stockTotal < detalleDto.CantidadUnidades)
                        throw new ValidacionException($"Stock insuficiente para el variante {detalleDto.VarianteId}. Disponible: {stockTotal}, Solicitado: {detalleDto.CantidadUnidades}");

                    foreach (var loteAlmacen in lotesAlmacen)
                    {
                        if (cantidadRestante <= 0) break;

                        var cantidadADescontar = Math.Min(loteAlmacen.StockDisponible, cantidadRestante);

                        loteAlmacen.StockDisponible -= cantidadADescontar;

                        if (loteAlmacen.StockDisponible == 0)
                        {
                            var stockTotalLote = await _loteAlmacenRepo.ObtenerStockTotalLoteAsync(loteAlmacen.LoteId);
                            loteAlmacen.Lote!.EstadoLote = stockTotalLote == 0
                                ? EstadoLote.Agotado
                                : EstadoLote.Activo;
                        }

                        await _loteAlmacenRepo.ActualizarAsync(loteAlmacen);

                        if (loteAlmacen.Lote != null)
                        {
                            loteAlmacen.Lote.CantidadVendida += cantidadADescontar;
                            await _loteRepo.ActualizarAsync(loteAlmacen.Lote);
                        }

                        var detalle = new VentaDetalle
                        {
                            VentaId = venta.Id,
                            VarianteId = detalleDto.VarianteId,
                            LoteId = loteAlmacen.LoteId,
                            CantidadUnidades = cantidadADescontar,
                            PrecioUnitarioCobrado = detalleDto.PrecioUnitarioCobrado,
                            CostoUnitarioLote = loteAlmacen.Lote?.CostoCompraUnitario ?? 0,
                            Subtotal = cantidadADescontar * detalleDto.PrecioUnitarioCobrado,
                            TipoPrecio = detalleDto.TipoPrecio
                        };

                        subtotalDetalle += detalle.Subtotal;
                        detallesCreados.Add(detalle);

                        var movimiento = new MovimientoInventario
                        {
                            LoteAlmacenId = loteAlmacen.Id,
                            VarianteId = detalleDto.VarianteId,
                            AlmacenOrigenId = loteAlmacen.AlmacenId,
                            AlmacenDestinoId = null,
                            TipoMovimiento = TipoMovimiento.SalidaVenta,
                            CantidadUnidades = cantidadADescontar,
                            SaldoResultante = loteAlmacen.StockDisponible,
                            ReferenciaId = venta.Id,
                            Observacion = $"Salida por venta #{venta.Id}"
                        };

                        await _movimientoRepo.CrearSinGuardarAsync(movimiento);

                        cantidadRestante -= cantidadADescontar;
                    }

                    if (cantidadRestante > 0)
                        throw new ValidacionException($"No se pudo descontar todo el stock para el variante {detalleDto.VarianteId}");

                    totalBruto += subtotalDetalle;
                }

                foreach (var detalle in detallesCreados)
                {
                    venta.Detalles.Add(detalle);
                }

                venta.TotalBruto = totalBruto;
                venta.TotalNeto = totalBruto - venta.DescuentoTotal;

                // El front manda los montos tal cual los tipeó el cajero:
                //   dto.MontoEfectivo      = billetes que ENTREGÓ el cliente
                //   dto.MontoTransferencia = lo que se transfirió
                // El backend calcula lo que se guarda:
                //   venta.MontoEfectivo = efectivo que QUEDA en caja (auditoría de caja)
                //   venta.MontoRecibido = billetes entregados; venta.Cambio = vuelto
                var total = venta.TotalNeto;
                var efectivoEntregado = dto.MontoEfectivo ?? 0;
                var transferencia = dto.MontoTransferencia ?? 0;

                if (efectivoEntregado < 0 || transferencia < 0)
                    throw new ValidacionException("Los montos de pago no pueden ser negativos");

                switch (dto.MetodoPago)
                {
                    case MetodoPago.Efectivo:
                    {
                        if (transferencia > 0)
                            throw new ValidacionException("En pago en efectivo el monto por transferencia debe ser 0. Use pago mixto");

                        if (efectivoEntregado <= 0)
                            efectivoEntregado = total;

                        if (efectivoEntregado < total)
                            throw new ValidacionException($"Monto insuficiente. Total a cobrar: {total:0.00}, recibido: {efectivoEntregado:0.00}, falta: {total - efectivoEntregado:0.00}");

                        venta.MontoEfectivo = total;
                        venta.MontoTransferencia = 0;
                        venta.MontoRecibido = efectivoEntregado;
                        venta.Cambio = efectivoEntregado - total;
                        break;
                    }
                    case MetodoPago.TransferenciaQR:
                    {
                        if (efectivoEntregado > 0)
                            throw new ValidacionException("En pago por transferencia el monto en efectivo debe ser 0. Use pago mixto");

                        if (transferencia > 0 && Math.Abs(transferencia - total) > 0.01m)
                            throw new ValidacionException($"El monto transferido ({transferencia:0.00}) debe ser igual al total a cobrar ({total:0.00})");

                        venta.MontoEfectivo = 0;
                        venta.MontoTransferencia = total;
                        venta.MontoRecibido = null;
                        venta.Cambio = null;
                        break;
                    }
                    case MetodoPago.Mixto:
                    {
                        if (efectivoEntregado <= 0 || transferencia <= 0)
                            throw new ValidacionException($"En pago mixto, el monto en efectivo y el monto por transferencia deben ser mayores a 0. Total a cobrar: {total:0.00}");

                        if (transferencia >= total)
                            throw new ValidacionException($"En pago mixto la transferencia ({transferencia:0.00}) debe ser menor al total a cobrar ({total:0.00}). Use pago por transferencia");

                        var efectivoACobrar = total - transferencia;
                        if (efectivoEntregado < efectivoACobrar)
                            throw new ValidacionException($"Monto insuficiente. Total a cobrar: {total:0.00}, recibido: {efectivoEntregado + transferencia:0.00}, falta: {efectivoACobrar - efectivoEntregado:0.00}");

                        venta.MontoEfectivo = efectivoACobrar;
                        venta.MontoTransferencia = transferencia;
                        venta.MontoRecibido = efectivoEntregado;
                        venta.Cambio = efectivoEntregado - efectivoACobrar;
                        break;
                    }
                    default:
                        throw new ValidacionException("Método de pago inválido. Valores permitidos: 1 (Efectivo), 2 (TransferenciaQR), 3 (Mixto)");
                }

                await _uow.SaveChangesAsync();

                sesion.IngresosEfectivo += venta.MontoEfectivo;
                sesion.IngresosDigitales += venta.MontoTransferencia;

                await _sesionRepo.ActualizarAsync(sesion);

                await _uow.CommitAsync();

                var ventaCompleta = await _ventaRepo.ObtenerConDetallesAsync(venta.Id);
                var resultado = _mapper.Map<VentaDto>(ventaCompleta!);
                resultado.FechaVenta = BoliviaTimeZone.ToLocal(venta.FechaVenta);
                resultado.CreatedAt = BoliviaTimeZone.ToLocal(venta.CreatedAt);
                return resultado;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear venta");
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var entidad = await _ventaRepo.ObtenerConDetallesAsync(id);
            if (entidad == null)
                throw new EntidadNoEncontradaException("Venta", id);

            ValidacionEntidad.VerificarActivo(entidad, "Venta");

            await _uow.BeginTransactionAsync();
            try
            {
                var almacenIdVenta = entidad.AlmacenId;
                foreach (var detalle in entidad.Detalles)
                {
                    var loteAlmacen = await _loteAlmacenRepo.ObtenerPorLoteYAlmacenAsync(detalle.LoteId, almacenIdVenta);

                    if (loteAlmacen != null)
                    {
                        loteAlmacen.StockDisponible += detalle.CantidadUnidades;

                        if (loteAlmacen.Lote != null && loteAlmacen.Lote.EstadoLote == EstadoLote.Agotado)
                        {
                            var stockTotalLote = await _loteAlmacenRepo.ObtenerStockTotalLoteAsync(loteAlmacen.LoteId);
                            if (stockTotalLote > 0)
                                loteAlmacen.Lote.EstadoLote = EstadoLote.Activo;
                        }

                        await _loteAlmacenRepo.ActualizarAsync(loteAlmacen);

                        if (loteAlmacen.Lote != null)
                        {
                            loteAlmacen.Lote.CantidadVendida -= detalle.CantidadUnidades;
                            await _loteRepo.ActualizarAsync(loteAlmacen.Lote);
                        }

                        var movimiento = new MovimientoInventario
                        {
                            LoteAlmacenId = loteAlmacen.Id,
                            VarianteId = detalle.VarianteId,
                            AlmacenOrigenId = null,
                            AlmacenDestinoId = loteAlmacen.AlmacenId,
                            TipoMovimiento = TipoMovimiento.DevolucionCliente,
                            CantidadUnidades = detalle.CantidadUnidades,
                            SaldoResultante = loteAlmacen.StockDisponible,
                            ReferenciaId = id,
                            Observacion = $"Devolución por anulación de venta #{id}"
                        };

                        await _movimientoRepo.CrearSinGuardarAsync(movimiento);
                    }
                }

                var sesion = await _sesionRepo.ObtenerPorIdAsync(entidad.SesionCajaId);
                if (sesion != null && sesion.Estado == EstadoSesionCaja.Abierta)
                {
                    sesion.IngresosEfectivo -= entidad.MontoEfectivo;
                    sesion.IngresosDigitales -= entidad.MontoTransferencia;

                    await _sesionRepo.ActualizarAsync(sesion);
                }

                await _ventaRepo.EliminarAsync(id);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular venta {Id}", id);
            throw;
        }
    }

    public async Task<VarianteVentaDto?> ObtenerVarianteVentaAsync(int varianteId)
    {
        try
        {
            var variante = await _varianteRepo.AsQueryable()
                .Where(v => v.Id == varianteId && v.IsActive)
                .Include(v => v.Producto)
                    .ThenInclude(p => p!.Fabricante)
                .Include(v => v.Sabor)
                .Include(v => v.Tamanio)
                .Include(v => v.Presentaciones)
                    .ThenInclude(p => p.TipoPresentacion)
                .FirstOrDefaultAsync();

            if (variante == null) return null;

            var cutoff = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(DateTime.UtcNow)).InicioUtc;

            var stockPorAlmacen = await (
                from la in _loteAlmacenRepo.AsQueryable()
                join l in _loteRepo.AsQueryable() on la.LoteId equals l.Id
                where la.IsActive
                    && l.IsActive
                    && l.VarianteId == varianteId
                    && l.EstadoLote != EstadoLote.Vencido
                    && l.EstadoLote != EstadoLote.Devuelto
                    && l.EstadoLote != EstadoLote.Baja
                    && (l.FechaVencimiento == null || l.FechaVencimiento >= cutoff)
                group la by la.AlmacenId into g
                select new { AlmacenId = g.Key, Stock = g.Sum(x => x.StockDisponible) })
                .Where(x => x.Stock > 0)
                .ToListAsync();

            var almacenIds = stockPorAlmacen.Select(x => x.AlmacenId).ToList();
            var almacenes = almacenIds.Count == 0
                ? []
                : await _almacenRepo.AsQueryable()
                    .Where(a => almacenIds.Contains(a.Id))
                    .ToListAsync();

            var almacenesDto = stockPorAlmacen
                .Select(s =>
                {
                    var almacen = almacenes.FirstOrDefault(a => a.Id == s.AlmacenId);
                    return new StockAlmacenVentaDto
                    {
                        AlmacenId = s.AlmacenId,
                        AlmacenNombre = almacen?.Nombre,
                        EsTienda = almacen?.EsTienda ?? false,
                        StockDisponible = s.Stock
                    };
                })
                .OrderBy(a => a.AlmacenNombre)
                .ToList();

            return new VarianteVentaDto
            {
                Id = variante.Id,
                ProductoId = variante.ProductoId,
                NombreProducto = variante.Producto?.Nombre,
                VarianteNombre = DescripcionProductoBuilder.Construir(
                    variante.Producto?.Nombre ?? "",
                    variante.Sabor?.Nombre ?? "",
                    variante.Tamanio?.Nombre ?? "",
                    variante.CantidadCaja,
                    variante.Producto?.Fabricante?.Nombre),
                CodigoBarras = variante.CodigoBarras,
                PrecioVentaUnitario = variante.PrecioVentaUnitario,
                PrecioVentaMayoreo = variante.PrecioVentaMayoreo,
                StockTotal = almacenesDto.Sum(a => a.StockDisponible),
                Almacenes = almacenesDto,
                Presentaciones = variante.Presentaciones
                    .Where(p => p.IsActive)
                    .Select(p => new PresentacionVarianteDto
                    {
                        Id = p.Id,
                        NombrePersonalizado = p.NombrePersonalizado,
                        Cantidad = p.CantidadDePadre,
                        Nombre = p.NombreMostrar,
                        Orden = p.TipoPresentacion!.Orden,
                        EsDefaultReporte = p.EsDefaultReporte
                    }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener variante para venta {VarianteId}", varianteId);
            throw;
        }
    }
}
