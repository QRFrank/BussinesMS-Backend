using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.Helpers;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Dominio.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Sistema;

public class ProductoVarianteService : IProductoVarianteService
{
    private readonly IProductoVarianteRepository _repo;
    private readonly IProductoRepository _productoRepo;
    private readonly IDescripcionSaborRepository _saborRepo;
    private readonly IDescripcionTamanioRepository _tamanioRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductoVarianteService> _logger;
    private readonly IProductoPresentacionRepository _presentacionRepo;
    private readonly IInventarioLoteAlmacenRepository _loteAlmacenRepo;

    public ProductoVarianteService(
        IProductoVarianteRepository repo,
        IProductoRepository productoRepo,
        IDescripcionSaborRepository saborRepo,
        IDescripcionTamanioRepository tamanioRepo,
        IProductoPresentacionRepository presentacionRepo,
        IMapper mapper,
        ILogger<ProductoVarianteService> logger,
        IInventarioLoteAlmacenRepository loteAlmacenRepo)
    {
        _repo = repo;
        _productoRepo = productoRepo;
        _saborRepo = saborRepo;
        _tamanioRepo = tamanioRepo;
        _presentacionRepo = presentacionRepo;
        _mapper = mapper;
        _logger = logger;
        _loteAlmacenRepo = loteAlmacenRepo;
    }

    public async Task<PagedResultDto<ProductoVarianteDto>> ObtenerTodosAsync(GenericPaginationQueryDto query)
    {
        try
        {
            IQueryable<ProductoVariante> baseQuery;
            var f = query.Filter?.ToLower();

            baseQuery = _repo.AsQueryable()
                .Where(x => x.IsActive)
                .Include(x => x.Producto)
                    .ThenInclude(p => p.Categoria)
                .Include(x => x.Sabor)
                .Include(x => x.Tamanio)
                .Include(x => x.Presentaciones.Where(p => p.IsActive))
                    .ThenInclude(p => p.TipoPresentacion);

            if (!string.IsNullOrWhiteSpace(f))
            {
                baseQuery = baseQuery.Where(x =>
                    x.Producto!.Nombre.ToLower().Contains(f) ||
                    (x.CodigoBarras != null && x.CodigoBarras.ToLower().Contains(f)) ||
                    x.Sabor!.Nombre.ToLower().Contains(f) ||
                    x.Tamanio!.Nombre.ToLower().Contains(f));
            }

            var camposNavegacion = new HashSet<string> { "categorianombre", "nombreproducto", "sabordescripcion", "pesotamanio" };
            var esNavegacion = camposNavegacion.Contains(query.SortBy?.ToLower() ?? "");

            int totalCount;
            IQueryable<ProductoVariante> filteredQuery;

            if (esNavegacion)
            {
                totalCount = await baseQuery.CountAsync();

                var page = query.GetPageValue();
                var pageSize = query.GetPageSizeValue();

                filteredQuery = AplicarOrdenamiento(baseQuery, query)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);
            }
            else
            {
                (filteredQuery, totalCount) = baseQuery.ApplyFilters(query);
            }

            var entidades = await filteredQuery.ToListAsync();
            var dtos = entidades.Select(e => MapearVarianteDto(e)).ToList();

            return new PagedResultDto<ProductoVarianteDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener variantes de producto");
            throw;
        }
    }

    public async Task<ProductoVarianteDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerConDetallesAsync(id);
            if (entidad == null || !entidad.IsActive) return null;

            var dto = MapearVarianteDto(entidad);

            dto.Presentaciones = entidad.Presentaciones
                .Where(p => p.IsActive)
                .OrderBy(p => p.TipoPresentacion?.Orden ?? 99)
                .Select(p =>
                {
                    var todas = entidad.Presentaciones.Where(x => x.IsActive).ToList();
                    var equivalencias = CalcularEquivalencias(todas);
                    return new ProductoPresentacionDto
                    {
                        Id = p.Id,
                        VarianteId = p.VarianteId,
                        TipoPresentacionId = p.TipoPresentacionId,
                        TipoNombre = p.TipoPresentacion?.Nombre ?? string.Empty,
                        NombrePersonalizado = p.NombrePersonalizado ?? string.Empty,
                        NombreMostrar = p.NombrePersonalizado ?? p.TipoPresentacion?.Nombre ?? string.Empty,
                        CantidadDePadre = p.CantidadDePadre,
                        PresentacionPadreId = p.PresentacionPadreId,
                        EquivalenciaEnUnidades = equivalencias.TryGetValue(p.Id, out var eq) ? eq : 1,
                        EsDefaultReporte = p.EsDefaultReporte,
                        CodigoBarras = p.CodigoBarras,
                        Orden = p.TipoPresentacion?.Orden ?? 0,
                        IsActive = p.IsActive,
                        CreatedAt = BoliviaTimeZone.ToLocal(p.CreatedAt)
                    };
                })
                .ToList();

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener variante de producto {Id}", id);
            throw;
        }
    }

    public async Task<ProductoVarianteDto?> ObtenerPorCodigoBarrasAsync(string codigoBarras)
    {
        try
        {
            var entidad = await _repo.ObtenerPorCodigoBarrasAsync(codigoBarras);
            if (entidad == null) return null;

            return MapearVarianteDto(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener variante por código de barras {CodigoBarras}", codigoBarras);
            throw;
        }
    }

    public async Task<CompraInfoVarianteDto?> ObtenerCompraInfoAsync(int id)
    {
        try
        {
            var entidad = await _repo.AsQueryable()
                .Include(x => x.Producto).ThenInclude(p => p.Categoria)
                .Include(x => x.Producto).ThenInclude(p => p.Fabricante)
                .Include(x => x.Sabor)
                .Include(x => x.Tamanio)
                .Include(x => x.Presentaciones).ThenInclude(p => p.TipoPresentacion)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (entidad == null) return null;

            var lotes = await _loteAlmacenRepo.AsQueryable()
                .Include(la => la.Lote)
                .Where(la => la.Lote!.VarianteId == id && la.StockDisponible > 0 && la.Lote!.EstadoLote == EstadoLote.Activo)
                .GroupBy(la => new { la.AlmacenId, la.Lote!.FechaVencimiento })
                .Select(g => new CompraInfoLoteDto
                {
                    AlmacenId = g.Key.AlmacenId,
                    FechaVencimiento = g.Key.FechaVencimiento.HasValue ? BoliviaTimeZone.ToLocal(g.Key.FechaVencimiento.Value) : (DateTime?)null,
                    StockDisponible = g.Sum(x => x.StockDisponible)
                })
                .ToListAsync();

            return new CompraInfoVarianteDto
            {
                Id = entidad.Id,
                ProductoId = entidad.ProductoId,
                DescripcionProducto = DescripcionProductoBuilder.Construir(
                    entidad.Producto?.Nombre ?? "", entidad.Sabor?.Nombre ?? "",
                    entidad.Tamanio?.Nombre ?? "", entidad.CantidadCaja,
                    entidad.Producto?.Fabricante?.Nombre),
                CodigoBarras = entidad.CodigoBarras,
                CategoriaId = entidad.Producto?.CategoriaId,
                CategoriaNombre = entidad.Producto?.Categoria?.Nombre,
                FabricanteId = entidad.Producto?.FabricanteId,
                FabricanteNombre = entidad.Producto?.Fabricante?.Nombre,
                PrecioCompra = entidad.PrecioCompra,
                PrecioVentaUnitario = entidad.PrecioVentaUnitario,
                PrecioVentaMayoreo = entidad.PrecioVentaMayoreo,
                CodigoAlmacen = entidad.CodigoAlmacen,
                Presentaciones = entidad.Presentaciones
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.TipoPresentacion?.Orden ?? 99)
                    .Select(p => new CompraInfoPresentacionDto
                    {
                        Id = p.Id,
                        NombrePersonalizado = p.NombrePersonalizado,
                        Cantidad = p.CantidadDePadre,
                        Nombre = p.TipoPresentacion?.Nombre,
                        Orden = p.TipoPresentacion?.Orden ?? 0,
                        EsDefaultReporte = p.EsDefaultReporte
                    }).ToList(),
                Lotes = lotes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener compra info de variante {Id}", id);
            throw;
        }
    }

    public async Task<ProductoVarianteDto> CrearAsync(CrearProductoVarianteDto dto)
    {
        try
        {
            var producto = await _productoRepo.ObtenerConDetallesAsync(dto.ProductoId);
            ValidacionEntidad.VerificarActivo(producto, "Producto");

            var sabor = await _saborRepo.ObtenerPorIdAsync(dto.SaborId);
            ValidacionEntidad.VerificarActivo(sabor, "Sabor");

            var tamanio = await _tamanioRepo.ObtenerPorIdAsync(dto.TamanioId);
            ValidacionEntidad.VerificarActivo(tamanio, "Tamaño");

            ValidacionEntidad.VerificarNoDuplicado(
                await _repo.ExisteCombinacionAsync(dto.ProductoId, dto.SaborId, dto.TamanioId),
                "Variante", $"'{producto!.Nombre} - {sabor!.Nombre} - {tamanio!.Nombre}'");

            ValidacionEntidad.VerificarNoDuplicado(
                await _repo.ExisteCodigoBarrasAsync(dto.CodigoBarras),
                "Código de barras", dto.CodigoBarras!);

            ValidarPresentaciones(dto.Presentaciones);

            var entidad = new ProductoVariante
            {
                ProductoId = dto.ProductoId,
                CodigoBarras = string.IsNullOrWhiteSpace(dto.CodigoBarras) ? null : dto.CodigoBarras,
                SaborId = dto.SaborId,
                TamanioId = dto.TamanioId,
                CantidadCaja = dto.CantidadCaja > 0 ? dto.CantidadCaja : null,
                PrecioVentaUnitario = dto.PrecioVentaUnitario,
                PrecioVentaMayoreo = dto.PrecioVentaMayoreo,
                PrecioCompra = dto.PrecioCompra,
                CodigoAlmacen = dto.CodigoAlmacen
            };

            var creada = await _repo.CrearAsync(entidad);

            await CrearPresentacionesAsync(creada.Id, dto.Presentaciones);

            _logger.LogInformation("Variante creada: Producto={ProductoId} Sabor={SaborId} Tamaño={TamanioId}",
                dto.ProductoId, dto.SaborId, dto.TamanioId);

            var completa = await _repo.ObtenerConDetallesAsync(creada.Id);
            return MapearVarianteDto(completa!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear variante de producto");
            throw;
        }
    }

    public async Task<ProductoVarianteDto> ActualizarAsync(ActualizarProductoVarianteDto dto)
    {
        try
        {
            var existente = await _repo.ObtenerConDetallesAsync(dto.Id);
            ValidacionEntidad.VerificarActivo(existente, "Variante de producto");

            var producto = await _productoRepo.ObtenerConDetallesAsync(dto.ProductoId);
            ValidacionEntidad.VerificarActivo(producto, "Producto");

            var sabor = await _saborRepo.ObtenerPorIdAsync(dto.SaborId);
            ValidacionEntidad.VerificarActivo(sabor, "Sabor");

            var tamanio = await _tamanioRepo.ObtenerPorIdAsync(dto.TamanioId);
            ValidacionEntidad.VerificarActivo(tamanio, "Tamaño");

            ValidacionEntidad.VerificarNoDuplicado(
                await _repo.ExisteCombinacionAsync(dto.ProductoId, dto.SaborId, dto.TamanioId, dto.Id),
                "Variante", $"'{producto!.Nombre} - {sabor!.Nombre} - {tamanio!.Nombre}'");

            ValidacionEntidad.VerificarNoDuplicado(
                await _repo.ExisteCodigoBarrasAsync(dto.CodigoBarras, dto.Id),
                "Código de barras", dto.CodigoBarras!);

            ValidarPresentaciones(dto.Presentaciones);

            existente!.ProductoId = dto.ProductoId;
            existente.CodigoBarras = string.IsNullOrWhiteSpace(dto.CodigoBarras) ? null : dto.CodigoBarras;
            existente.SaborId = dto.SaborId;
            existente.TamanioId = dto.TamanioId;
            existente.CantidadCaja = dto.CantidadCaja;
            existente.PrecioVentaUnitario = dto.PrecioVentaUnitario;
            existente.PrecioVentaMayoreo = dto.PrecioVentaMayoreo;
            existente.PrecioCompra = dto.PrecioCompra;
            existente.CodigoAlmacen = dto.CodigoAlmacen;
            existente.IsActive = dto.IsActive;

            await _repo.ActualizarAsync(existente);

            await _presentacionRepo.EliminarPorVarianteAsync(existente.Id);
            await CrearPresentacionesAsync(existente.Id, dto.Presentaciones);

            _logger.LogInformation("Variante actualizada: {Id}", dto.Id);

            var completa = await _repo.ObtenerConDetallesAsync(existente.Id);
            return MapearVarianteDto(completa!);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar variante de producto {Id}", dto.Id);
            throw;
        }
    }
    public async Task EliminarAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            ValidacionEntidad.VerificarActivo(existente, "Variante de producto");

            await _repo.EliminarAsync(id);
            _logger.LogInformation("Variante de producto eliminada: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar variante de producto {Id}", id);
            throw;
        }
    }

    private async Task CrearPresentacionesAsync(int varianteId, List<CrearPresentacionEnVarianteDto> presentaciones)
    {
        var idsPorIndice = new Dictionary<int, int>();

        for (int i = 0; i < presentaciones.Count; i++)
        {
            var p = presentaciones[i];

            int? padreIdReal = null;
            if (p.PresentacionPadreIndice.HasValue)
            {
                if (!idsPorIndice.TryGetValue(p.PresentacionPadreIndice.Value, out var padreId))
                    throw new InvalidOperationException(
                        $"PresentacionPadreIndice={p.PresentacionPadreIndice} no encontrado. " +
                        "Asegúrate de que el padre aparezca antes en el array.");
                padreIdReal = padreId;
            }

            var entidad = new ProductoPresentacion
            {
                VarianteId = varianteId,
                TipoPresentacionId = p.TipoPresentacionId,
                NombrePersonalizado = p.NombrePersonalizado,
                CantidadDePadre = p.CantidadDePadre,
                PresentacionPadreId = padreIdReal,
                EsDefaultReporte = p.EsDefaultReporte,
                CodigoBarras = p.CodigoBarras
            };

            var creada = await _presentacionRepo.CrearAsync(entidad);
            idsPorIndice[i] = creada.Id;
        }
    }

    private static void ValidarPresentaciones(List<CrearPresentacionEnVarianteDto> presentaciones)
    {
        if (!presentaciones.Any())
            throw new InvalidOperationException("La variante debe tener al menos una presentación (Unidad).");

        var raices = presentaciones.Count(p => p.PresentacionPadreIndice == null);
        if (raices != 1)
            throw new InvalidOperationException("Debe haber exactamente una presentación raíz (Unidad) sin padre.");

        var defaults = presentaciones.Count(p => p.EsDefaultReporte);
        if (defaults != 1)
            throw new InvalidOperationException("Debe haber exactamente una presentación marcada como default de reporte.");

        for (int i = 0; i < presentaciones.Count; i++)
        {
            var indice = presentaciones[i].PresentacionPadreIndice;
            if (indice.HasValue && (indice.Value < 0 || indice.Value >= i))
                throw new InvalidOperationException(
                    $"PresentacionPadreIndice={indice} inválido en posición {i}. " +
                    "El padre debe aparecer antes en el array.");
        }
    }

    private static ProductoVarianteDto MapearVarianteDto(ProductoVariante e)
    {
        var pres = e.Presentaciones
            .OrderBy(p => p.TipoPresentacion?.Orden ?? 99)
            .ToList();

        var equivalencias = CalcularEquivalencias(pres);

        var dto = new ProductoVarianteDto
        {
            Id = e.Id,
            ProductoId = e.ProductoId,
            NombreProducto = e.Producto?.Nombre,
            DescripcionProducto = DescripcionProductoBuilder.Construir(
                e.Producto?.Nombre ?? "", e.Sabor?.Nombre ?? "", e.Tamanio?.Nombre ?? "",
                e.CantidadCaja, e.Producto?.Fabricante?.Nombre),
            CodigoBarras = e.CodigoBarras,
            SaborId = e.SaborId,
            SaborDescripcion = e.Sabor?.Nombre,
            TamanioId = e.TamanioId,
            CantidadCaja = e.CantidadCaja,
            PesoTamanio = e.Tamanio?.Nombre,
            PrecioVentaUnitario = e.PrecioVentaUnitario,
            PrecioVentaMayoreo = e.PrecioVentaMayoreo,
            PrecioCompra = e.PrecioCompra,
            CodigoAlmacen = e.CodigoAlmacen,
            IsActive = e.IsActive,
            CreatedAt = BoliviaTimeZone.ToLocal(e.CreatedAt),
            CategoriaId = e.Producto?.CategoriaId,
            CategoriaNombre = e.Producto?.Categoria?.Nombre,
        };

        if (pres.Count > 0)
        {
            var p1 = pres[0];
            dto.PresentacionId1 = p1.Id;
            dto.TipoNombre1 = p1.TipoPresentacion?.Nombre;
            dto.NombrePersonalizado1 = p1.NombrePersonalizado;
            dto.EquivalenciaEnUnidades1 = equivalencias.TryGetValue(p1.Id, out var eq1) ? eq1 : 1;
            dto.EsDefaultReporte1 = p1.EsDefaultReporte;
        }

        if (pres.Count > 1)
        {
            var p2 = pres[1];
            dto.PresentacionId2 = p2.Id;
            dto.TipoNombre2 = p2.TipoPresentacion?.Nombre;
            dto.NombrePersonalizado2 = p2.NombrePersonalizado;
            dto.EquivalenciaEnUnidades2 = equivalencias.TryGetValue(p2.Id, out var eq2) ? eq2 : 1;
            dto.EsDefaultReporte2 = p2.EsDefaultReporte;
        }

        if (pres.Count > 2)
        {
            var p3 = pres[2];
            dto.PresentacionId3 = p3.Id;
            dto.TipoNombre3 = p3.TipoPresentacion?.Nombre;
            dto.NombrePersonalizado3 = p3.NombrePersonalizado;
            dto.EquivalenciaEnUnidades3 = equivalencias.TryGetValue(p3.Id, out var eq3) ? eq3 : 1;
            dto.EsDefaultReporte3 = p3.EsDefaultReporte;
        }

        return dto;
    }

    private static Dictionary<int, int> CalcularEquivalencias(List<ProductoPresentacion> presentaciones)
    {
        var resultado = new Dictionary<int, int>();
        foreach (var pres in presentaciones)
            resultado[pres.Id] = CalcularEquivalenciaRecursiva(pres, presentaciones);
        return resultado;
    }

    private static int CalcularEquivalenciaRecursiva(
        ProductoPresentacion pres,
        List<ProductoPresentacion> todas)
    {
        if (pres.PresentacionPadreId == null)
            return 1;

        var padre = todas.FirstOrDefault(x => x.Id == pres.PresentacionPadreId);
        if (padre == null)
            return pres.CantidadDePadre;

        return pres.CantidadDePadre * CalcularEquivalenciaRecursiva(padre, todas);
    }

    private static IQueryable<ProductoVariante> AplicarOrdenamiento(
    IQueryable<ProductoVariante> query,
    GenericPaginationQueryDto paginationQuery)
    {
        var desc = paginationQuery.SortDirection?.ToLower() == "desc";

        return paginationQuery.SortBy?.ToLower() switch
        {
            "categorianombre" => desc
                ? query.OrderByDescending(x => x.Producto!.Categoria!.Nombre)
                : query.OrderBy(x => x.Producto!.Categoria!.Nombre),

            "nombreproducto" => desc
                ? query.OrderByDescending(x => x.Producto!.Nombre)
                : query.OrderBy(x => x.Producto!.Nombre),

            "sabordescripcion" => desc
                ? query.OrderByDescending(x => x.Sabor!.Nombre)
                : query.OrderBy(x => x.Sabor!.Nombre),

            "pesotamanio" => desc
                ? query.OrderByDescending(x => x.Tamanio!.Nombre)
                : query.OrderBy(x => x.Tamanio!.Nombre),

            _ => query
        };
    }
}
