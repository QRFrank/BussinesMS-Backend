using AutoMapper;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Helpers;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Dominio.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Sistema;

public class VarianteStockService : IVarianteStockService
{
    private readonly IProductoVarianteRepository _varianteRepo;
    private readonly IInventarioLoteRepository _loteRepo;
    private readonly IInventarioLoteAlmacenRepository _loteAlmacenRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<VarianteStockService> _logger;

    public VarianteStockService(
        IProductoVarianteRepository varianteRepo,
        IInventarioLoteRepository loteRepo,
        IInventarioLoteAlmacenRepository loteAlmacenRepo,
        IMapper mapper,
        ILogger<VarianteStockService> logger)
    {
        _varianteRepo = varianteRepo;
        _loteRepo = loteRepo;
        _loteAlmacenRepo = loteAlmacenRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<VarianteStockDto>> ObtenerStockAsync(GenericPaginationQueryDto query)
    {
        try
        {
            var loteQuery = _loteRepo.AsQueryable().Where(l => l.IsActive);

            var variantesQuery = _varianteRepo.AsQueryable()
                .Where(v => v.IsActive)
                .Include(v => v.Producto)
                    .ThenInclude(p => p!.Categoria)
                .Include(v => v.Producto)
                    .ThenInclude(p => p!.Fabricante)
                .Include(v => v.Sabor)
                .Include(v => v.Tamanio)
                .Include(v => v.Presentaciones)
                    .ThenInclude(p => p.TipoPresentacion);

            var baseQuery = from v in variantesQuery
                            join l in loteQuery on v.Id equals l.VarianteId into loteGroup
                            select new VarianteStockDto
                            {
                                Id = v.Id,
                                ProductoId = v.ProductoId,
                                NombreProducto = v.Producto!.Nombre,
                                VarianteNombre = DescripcionProductoBuilder.Construir(
                                    v.Producto.Nombre,
                                    v.Sabor!.Nombre,
                                    v.Tamanio!.Nombre,
                                    v.CantidadCaja,
                                    v.Producto.Fabricante!.Nombre),
                                CodigoBarras = v.CodigoBarras,
                                PrecioVentaUnitario = v.PrecioVentaUnitario,
                                PrecioVentaMayoreo = v.PrecioVentaMayoreo,
                                PrecioCompra = v.PrecioCompra,
                                CodigoAlmacen = v.CodigoAlmacen,
                                IsActive = v.IsActive,
                                CategoriaId = v.Producto.CategoriaId,
                                CategoriaNombre = v.Producto.Categoria!.Nombre,
                                StockDisponible = loteGroup
                                    .SelectMany(l => _loteAlmacenRepo.AsQueryable()
                                        .Where(la => la.LoteId == l.Id && la.IsActive))
                                    .Sum(la => la.StockDisponible),
                                CantidadVendida = loteGroup.Sum(l => l.CantidadVendida),
                                CantidadVencida = loteGroup.Sum(l => l.CantidadVencida),
                                Presentaciones = v.Presentaciones
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

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    (x.NombreProducto != null && x.NombreProducto.ToLower().Contains(f)) ||
                    (x.CodigoBarras != null && x.CodigoBarras.ToLower().Contains(f)) ||
                    (x.CategoriaNombre != null && x.CategoriaNombre.ToLower().Contains(f)));
            }

            var (filteredQuery, totalCount) = baseQuery.ApplyFilters(query);

            var items = await filteredQuery.ToListAsync();

            return new PagedResultDto<VarianteStockDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener stock de variantes");
            throw;
        }
    }

    public async Task<PagedResultDto<VarianteStockPosDto>> ObtenerStockPosAsync(GenericPaginationQueryDto query, int almacenId)
    {
        try
        {
            var cutoff = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(DateTime.UtcNow)).InicioUtc;

            var loteQuery = _loteRepo.AsQueryable().Where(l => l.IsActive);

            var variantesQuery = _varianteRepo.AsQueryable()
                .Where(v => v.IsActive)
                .Include(v => v.Producto)
                    .ThenInclude(p => p!.Categoria)
                .Include(v => v.Producto)
                    .ThenInclude(p => p!.Fabricante)
                .Include(v => v.Sabor)
                .Include(v => v.Tamanio)
                .Include(v => v.Presentaciones)
                    .ThenInclude(p => p.TipoPresentacion);

            var baseQuery = from v in variantesQuery
                            join l in loteQuery on v.Id equals l.VarianteId into loteGroup
                            select new VarianteStockPosDto
                            {
                                Id = v.Id,
                                ProductoId = v.ProductoId,
                                NombreProducto = v.Producto!.Nombre,
                                VarianteNombre = DescripcionProductoBuilder.Construir(
                                    v.Producto.Nombre,
                                    v.Sabor!.Nombre,
                                    v.Tamanio!.Nombre,
                                    v.CantidadCaja,
                                    v.Producto.Fabricante!.Nombre),
                                CodigoBarras = v.CodigoBarras,
                                PrecioVentaUnitario = v.PrecioVentaUnitario,
                                PrecioVentaMayoreo = v.PrecioVentaMayoreo,
                                CategoriaId = v.Producto.CategoriaId,
                                CategoriaNombre = v.Producto.Categoria!.Nombre,
                                Cantidad = loteGroup
                                    .SelectMany(l => _loteAlmacenRepo.AsQueryable()
                                        .Where(la => la.LoteId == l.Id
                                            && la.IsActive
                                            && la.AlmacenId == almacenId
                                            && l.EstadoLote != EstadoLote.Vencido
                                            && l.EstadoLote != EstadoLote.Devuelto
                                            && l.EstadoLote != EstadoLote.Baja
                                            && (l.FechaVencimiento == null || l.FechaVencimiento >= cutoff)))
                                    .Sum(la => la.StockDisponible),
                                Presentaciones = v.Presentaciones
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

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    (x.NombreProducto != null && x.NombreProducto.ToLower().Contains(f)) ||
                    (x.CodigoBarras != null && x.CodigoBarras.ToLower().Contains(f)) ||
                    (x.CategoriaNombre != null && x.CategoriaNombre.ToLower().Contains(f)));
            }

            baseQuery = baseQuery.Where(x => x.Cantidad > 0);

            baseQuery = baseQuery.OrderBy(x => x.NombreProducto);

            var (filteredQuery, totalCount) = baseQuery.ApplyFilters(query, skipSorting: true);

            var items = await filteredQuery.ToListAsync();

            return new PagedResultDto<VarianteStockPosDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener stock POS de variantes para almacen {AlmacenId}", almacenId);
            throw;
        }
    }

    public async Task<VarianteStockDetalleDto?> ObtenerStockDetalleAsync(int varianteId)
    {
        try
        {
            var variante = await _varianteRepo.AsQueryable()
                .Where(v => v.Id == varianteId && v.IsActive)
                .Include(v => v.Producto)
                    .ThenInclude(p => p!.Categoria)
                .Include(v => v.Producto)
                    .ThenInclude(p => p!.Fabricante)
                .Include(v => v.Sabor)
                .Include(v => v.Tamanio)
                .Include(v => v.Presentaciones)
                    .ThenInclude(p => p.TipoPresentacion)
                .FirstOrDefaultAsync();

            if (variante == null) return null;

            var lotes = await _loteRepo.AsQueryable()
                .Where(l => l.VarianteId == varianteId && l.IsActive)
                .Include(l => l.Variante)
                .OrderBy(l => l.FechaVencimiento)
                .ToListAsync();

            var lotesDto = new List<LoteAlmacenStockDto>();
            foreach (var lote in lotes)
            {
                var almacenes = await _loteAlmacenRepo.AsQueryable()
                    .Where(la => la.LoteId == lote.Id && la.IsActive)
                    .ToListAsync();

                foreach (var alm in almacenes)
                {
                    int? diasParaVencer = null;
                    if (lote.FechaVencimiento.HasValue)
                    {
                        diasParaVencer = (int)(lote.FechaVencimiento.Value - DateTime.UtcNow).TotalDays;
                    }

                    lotesDto.Add(new LoteAlmacenStockDto
                    {
                        Id = lote.Id,
                        CodigoLote = lote.CodigoLote,
                        AlmacenId = alm.AlmacenId,
                        StockInicial = lote.StockInicial,
                        StockDisponible = alm.StockDisponible,
                        CantidadVendida = lote.CantidadVendida,
                        CantidadVencida = lote.CantidadVencida,
                        EstadoLote = (int)lote.EstadoLote,
                        CompraDetalleId = lote.CompraDetalleId,
                        CostoCompraUnitario = lote.CostoCompraUnitario,
                        FechaVencimiento = lote.FechaVencimiento,
                        DiasParaVencer = diasParaVencer,
                        IsActive = alm.IsActive,
                        CreatedAt = alm.CreatedAt
                    });
                }
            }

            return new VarianteStockDetalleDto
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
                PrecioCompra = variante.PrecioCompra,
                CodigoAlmacen = variante.CodigoAlmacen,
                IsActive = variante.IsActive,
                CategoriaId = variante.Producto?.CategoriaId,
                CategoriaNombre = variante.Producto?.Categoria?.Nombre,
                StockDisponible = lotesDto.Sum(l => l.StockDisponible),
                CantidadVendida = lotes.Sum(l => l.CantidadVendida),
                CantidadVencida = lotes.Sum(l => l.CantidadVencida),
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
                    }).ToList(),
                Lotes = lotesDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener detalle de stock de variante {VarianteId}", varianteId);
            throw;
        }
    }
}
