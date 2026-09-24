using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Helpers;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Dominio.Enums;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Sistema;

public class InventarioLoteService : IInventarioLoteService
{
    private readonly IInventarioLoteRepository _loteRepo;
    private readonly IInventarioLoteAlmacenRepository _loteAlmacenRepo;
    private readonly IMovimientoInventarioRepository _movimientoRepo;
    private readonly IProductoVarianteRepository _varianteRepo;
    private readonly ISistemaUnitOfWork _uow;
    private readonly ILogger<InventarioLoteService> _logger;

    public InventarioLoteService(
        IInventarioLoteRepository loteRepo,
        IInventarioLoteAlmacenRepository loteAlmacenRepo,
        IMovimientoInventarioRepository movimientoRepo,
        IProductoVarianteRepository varianteRepo,
        ISistemaUnitOfWork uow,
        ILogger<InventarioLoteService> logger)
    {
        _loteRepo = loteRepo;
        _loteAlmacenRepo = loteAlmacenRepo;
        _movimientoRepo = movimientoRepo;
        _varianteRepo = varianteRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<PagedResultDto<InventarioLoteDto>> ObtenerTodosAsync(
        GenericPaginationQueryDto query, int? categoriaId = null, int? almacenId = null, int? estadoLote = null)
    {
        try
        {
            var baseQuery = _loteRepo.AsQueryable()
                .Include(l => l.Variante)
                    .ThenInclude(v => v!.Producto)
                        .ThenInclude(p => p!.Categoria)
                .Include(l => l.Variante!.Producto!.Fabricante)
                .Include(l => l.Variante!.Sabor)
                .Include(l => l.Variante!.Tamanio)
                .Include(l => l.Variante!.Presentaciones)
                    .ThenInclude(p => p.TipoPresentacion)
                .Where(l => l.IsActive);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(l =>
                    l.Variante != null &&
                    ((l.Variante.Producto != null &&
                      l.Variante.Producto.Nombre.ToLower().Contains(f)) ||
                     (l.Variante.CodigoBarras != null &&
                      l.Variante.CodigoBarras.ToLower().Contains(f))));
            }

            if (categoriaId.HasValue)
            {
                baseQuery = baseQuery.Where(l =>
                    l.Variante != null &&
                    l.Variante.Producto != null &&
                    l.Variante.Producto.CategoriaId == categoriaId.Value);
            }

            if (estadoLote.HasValue)
            {
                baseQuery = baseQuery.Where(l => l.EstadoLote == (EstadoLote)estadoLote.Value);
            }

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query);
            var lotes = await filteredQuery.ToListAsync();

            if (almacenId.HasValue)
            {
                var loteIds = lotes.Select(l => l.Id).ToList();
                var almacenesDelLote = await _loteAlmacenRepo.AsQueryable()
                    .Where(la => loteIds.Contains(la.LoteId)
                             && la.AlmacenId == almacenId.Value
                             && la.IsActive)
                    .ToListAsync();

                var loteIdsConAlmacen = almacenesDelLote.Select(la => la.LoteId).ToHashSet();
                lotes = lotes.Where(l => loteIdsConAlmacen.Contains(l.Id)).ToList();
                totalCount = lotes.Count;
            }

            var dtos = new List<InventarioLoteDto>();
            foreach (var lote in lotes)
            {
                var almacenesQuery = _loteAlmacenRepo.AsQueryable()
                    .Where(la => la.LoteId == lote.Id && la.IsActive);

                if (almacenId.HasValue)
                {
                    almacenesQuery = almacenesQuery.Where(la => la.AlmacenId == almacenId.Value);
                }

                var almacenes = await almacenesQuery.ToListAsync();

                dtos.Add(MapToDto(lote, almacenes));
            }

            return new PagedResultDto<InventarioLoteDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lotes de inventario");
            throw;
        }
    }

    public async Task<InventarioLoteDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var lote = await _loteRepo.ObtenerConDetallesAsync(id);
            if (lote == null || !lote.IsActive) return null;

            var almacenes = await _loteAlmacenRepo.AsQueryable()
                .Where(la => la.LoteId == lote.Id && la.IsActive)
                .ToListAsync();

            return MapToDto(lote, almacenes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lote de inventario {Id}", id);
            throw;
        }
    }

    public async Task<InventarioLoteDto> CrearAsync(CrearInventarioLoteAlmacenDto dto)
    {
        try
        {
            if (dto.StockInicial <= 0)
                throw new ValidacionException("El stock inicial debe ser mayor a 0");

            await _uow.BeginTransactionAsync();
            try
            {
                var codigoLote = await GenerarCodigoLoteUnicoAsync(dto.VarianteId);

                var lote = new InventarioLote
                {
                    VarianteId = dto.VarianteId,
                    CompraDetalleId = dto.CompraDetalleId,
                    CodigoLote = codigoLote,
                    CostoCompraUnitario = dto.CostoCompraUnitario,
                    StockInicial = dto.StockInicial,
                    CantidadVendida = 0,
                    CantidadVencida = 0,
                    EstadoLote = EstadoLote.Activo,
                    FechaVencimiento = dto.FechaVencimiento
                };
                var loteCreado = await _loteRepo.CrearSinGuardarAsync(lote);

                var loteAlmacen = new InventarioLoteAlmacen
                {
                    LoteId = loteCreado.Id,
                    VarianteId = dto.VarianteId,
                    AlmacenId = dto.AlmacenId,
                    StockDisponible = dto.StockInicial
                };
                var creado = await _loteAlmacenRepo.CrearSinGuardarAsync(loteAlmacen);

                var movimiento = new MovimientoInventario
                {
                    LoteAlmacenId = creado.Id,
                    VarianteId = dto.VarianteId,
                    AlmacenOrigenId = dto.AlmacenId,
                    TipoMovimiento = TipoMovimiento.EntradaCompra,
                    CantidadUnidades = dto.StockInicial,
                    SaldoResultante = dto.StockInicial,
                    ReferenciaId = dto.CompraDetalleId,
                    Observacion = "Creación de lote"
                };
                await _movimientoRepo.CrearSinGuardarAsync(movimiento);

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                var loteCompleto = await _loteRepo.ObtenerConDetallesAsync(loteCreado.Id);
                var almacenes = await _loteAlmacenRepo.AsQueryable()
                    .Where(la => la.LoteId == loteCreado.Id && la.IsActive)
                    .ToListAsync();

                _logger.LogInformation(
                    "Lote de inventario creado: LoteId={Id} - Variante: {VarianteId}",
                    loteCreado.Id, dto.VarianteId);

                return MapToDto(loteCompleto!, almacenes);
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear lote de inventario");
            throw;
        }
    }

    public async Task<InventarioLoteDto> ActualizarAsync(ActualizarInventarioLoteAlmacenDto dto)
    {
        try
        {
            var loteAlmacen = await _loteAlmacenRepo.ObtenerPorIdAsync(dto.Id);
            ValidacionEntidad.VerificarActivo(loteAlmacen, "InventarioLoteAlmacen");

            loteAlmacen!.StockDisponible = dto.StockDisponible;

            var lote = await _loteRepo.ObtenerPorIdAsync(loteAlmacen.LoteId);
            if (lote != null)
            {
                lote.EstadoLote = (EstadoLote)dto.EstadoLote;
                await _loteRepo.ActualizarAsync(lote);
            }

            loteAlmacen.IsActive = dto.IsActive;

            var actualizada = await _loteAlmacenRepo.ActualizarAsync(loteAlmacen);

            _logger.LogInformation("Lote de inventario actualizado: {Id}", actualizada.Id);

            var loteCompleto = await _loteRepo.ObtenerConDetallesAsync(loteAlmacen.LoteId);
            var almacenes = await _loteAlmacenRepo.AsQueryable()
                .Where(la => la.LoteId == loteAlmacen.LoteId && la.IsActive)
                .ToListAsync();

            return MapToDto(loteCompleto!, almacenes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar lote {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var loteAlmacen = await _loteAlmacenRepo.ObtenerPorIdAsync(id);
            ValidacionEntidad.VerificarActivo(loteAlmacen, "InventarioLoteAlmacen");

            await _loteAlmacenRepo.EliminarAsync(id);
            _logger.LogInformation("Lote de inventario eliminado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar lote {Id}", id);
            throw;
        }
    }

    public async Task<InventarioLoteDto> AjustarStockAsync(int id, AjusteInventarioDto dto)
    {
        try
        {
            var loteAlmacen = await _loteAlmacenRepo.ObtenerPorIdAsync(id);
            ValidacionEntidad.VerificarActivo(loteAlmacen, "InventarioLoteAlmacen");

            await _uow.BeginTransactionAsync();
            try
            {
                var stockAnterior = loteAlmacen!.StockDisponible;
                loteAlmacen.StockDisponible += dto.CantidadAjuste;

                if (loteAlmacen.StockDisponible < 0)
                    throw new ValidacionException("El ajuste resultaría en stock negativo");

                var lote = await _loteRepo.ObtenerPorIdAsync(loteAlmacen.LoteId);
                if (lote != null)
                {
                    var sumaStockAlmacenes = await _loteAlmacenRepo.AsQueryable()
                        .Where(la => la.LoteId == lote.Id && la.IsActive)
                        .SumAsync(la => la.StockDisponible);

                    if (sumaStockAlmacenes > lote.StockInicial)
                        throw new ValidacionException(
                            "La suma de stock en almacenes supera el stock inicial del lote");

                    if (loteAlmacen.StockDisponible == 0)
                        lote.EstadoLote = EstadoLote.Agotado;
                    else if (loteAlmacen.StockDisponible > 0 &&
                             lote.EstadoLote == EstadoLote.Agotado)
                        lote.EstadoLote = EstadoLote.Activo;

                    await _loteRepo.ActualizarAsync(lote);
                }

                await _loteAlmacenRepo.ActualizarAsync(loteAlmacen);

                var tipoMovimiento = dto.CantidadAjuste > 0
                    ? TipoMovimiento.AjustePositivo
                    : TipoMovimiento.AjusteNegativo;

                var movimiento = new MovimientoInventario
                {
                    LoteAlmacenId = loteAlmacen.Id,
                    VarianteId = loteAlmacen.Lote?.VarianteId ?? 0,
                    AlmacenOrigenId = loteAlmacen.AlmacenId,
                    TipoMovimiento = tipoMovimiento,
                    CantidadUnidades = Math.Abs(dto.CantidadAjuste),
                    SaldoResultante = loteAlmacen.StockDisponible,
                    Observacion = dto.Observacion ??
                        $"Ajuste de stock: {stockAnterior} → {loteAlmacen.StockDisponible}"
                };
                await _movimientoRepo.CrearSinGuardarAsync(movimiento);

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                var loteCompleto = await _loteRepo.ObtenerConDetallesAsync(loteAlmacen.LoteId);
                var almacenes = await _loteAlmacenRepo.AsQueryable()
                    .Where(la => la.LoteId == loteAlmacen.LoteId && la.IsActive)
                    .ToListAsync();

                _logger.LogInformation(
                    "Ajuste de stock en lote {Id}: {Anterior} → {Nuevo}",
                    id, stockAnterior, loteAlmacen.StockDisponible);

                return MapToDto(loteCompleto!, almacenes);
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al ajustar stock en lote {Id}", id);
            throw;
        }
    }

    private async Task<string> GenerarCodigoLoteUnicoAsync(int varianteId)
    {
        var variante = await _varianteRepo.ObtenerConDetallesAsync(varianteId)
            ?? throw new ValidacionException($"Variante {varianteId} no encontrada");

        var nombreProducto = variante.Producto?.Nombre ?? string.Empty;

        var textoPresentacion = variante.Tamanio?.Nombre ?? "UN";

        var fechaLocal = BoliviaTimeZone.Now();
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

    private static InventarioLoteDto MapToDto(InventarioLote lote, List<InventarioLoteAlmacen> almacenes)
    {
        var variante = lote.Variante;
        var producto = variante?.Producto;
        var categoria = producto?.Categoria;

        int? diasParaVencer = null;
        if (lote.FechaVencimiento.HasValue)
        {
            diasParaVencer = (int)(lote.FechaVencimiento.Value - DateTime.UtcNow).TotalDays;
        }

        return new InventarioLoteDto
        {
            Id = lote.Id,
            VarianteId = variante?.Id ?? 0,
            CodigoLote = lote.CodigoLote,
            VarianteNombre = DescripcionProductoBuilder.Construir(
                variante?.Producto?.Nombre ?? "", variante?.Sabor?.Nombre ?? "",
                variante?.Tamanio?.Nombre ?? "", variante?.CantidadCaja,
                variante?.Producto?.Fabricante?.Nombre),
            NombreProducto = producto?.Nombre,
            CodigoBarras = variante?.CodigoBarras,
            CategoriaId = categoria?.Id ?? 0,
            CategoriaNombre = categoria?.Nombre,
            StockInicial = lote.StockInicial,
            CantidadVendida = lote.CantidadVendida,
            CantidadVencida = lote.CantidadVencida,
            EstadoLote = (int)lote.EstadoLote,
            CostoCompraUnitario = lote.CostoCompraUnitario,
            PrecioVentaUnitario = variante?.PrecioVentaUnitario ?? 0,
            PrecioVentaMayoreo = variante?.PrecioVentaMayoreo ?? 0,
            FechaVencimiento = lote.FechaVencimiento,
            DiasParaVencer = diasParaVencer,
            CompraDetalleId = lote.CompraDetalleId,
            IsActive = lote.IsActive,
            CreatedAt = lote.CreatedAt,
            Presentaciones = variante?.Presentaciones?
                .Where(p => p.IsActive)
                .Select(p => new PresentacionVarianteDto
                {
                    Id = p.Id,
                    NombrePersonalizado = p.NombrePersonalizado,
                    Cantidad = p.CantidadDePadre,
                    Nombre = p.NombreMostrar,
                    Orden = p.TipoPresentacion!.Orden,
                    EsDefaultReporte = p.EsDefaultReporte
                }).ToList() ?? [],
            Almacenes = almacenes.Where(la => la.StockDisponible > 0).Select(la => new AlmacenLoteDto
            {
                InventarioLoteAlmacenId = la.Id,
                AlmacenId = la.AlmacenId,
                StockDisponible = la.StockDisponible
            }).ToList()
        };
    }
}
