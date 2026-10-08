using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductoNav = BussinesMS.Dominio.Entidades.Navidad.Producto;
using ProductoPresentacionNav = BussinesMS.Dominio.Entidades.Navidad.ProductoPresentacion;
using ProveedorNav = BussinesMS.Dominio.Entidades.Navidad.Proveedor;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Nota: CreatedAt se convierte a hora de Bolivia en NavidadMappingProfile (no reconvertir aquí).
public class ProductoNavService : IProductoNavService
{
    private const string NombreUnidadPorDefecto = "Unidad";

    private readonly IProductoNavRepository _repo;
    private readonly IProductoPresentacionNavRepository _presentacionRepo;
    private readonly IProveedorNavRepository _proveedorRepo;
    private readonly ICategoriaProductoNavRepository _categoriaRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductoNavService> _logger;

    public ProductoNavService(
        IProductoNavRepository repo,
        IProductoPresentacionNavRepository presentacionRepo,
        IProveedorNavRepository proveedorRepo,
        ICategoriaProductoNavRepository categoriaRepo,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        IMapper mapper,
        ILogger<ProductoNavService> logger)
    {
        _repo = repo;
        _presentacionRepo = presentacionRepo;
        _proveedorRepo = proveedorRepo;
        _categoriaRepo = categoriaRepo;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<ProductoNavDto>> ObtenerTodosAsync(ProductoNavFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var baseQuery = _repo.AsQueryable()
                .Include(x => x.Proveedor)
                .Include(x => x.Categoria)
                .Include(x => x.Presentaciones.Where(p => p.IsActive).OrderBy(p => p.Unidades))
                .Where(x => x.IsActive && x.TemporadaId == temporadaId);

            if (query.ProveedorId.HasValue)
                baseQuery = baseQuery.Where(x => x.ProveedorId == query.ProveedorId.Value);

            if (query.CategoriaProductoId.HasValue)
                baseQuery = baseQuery.Where(x => x.CategoriaProductoId == query.CategoriaProductoId.Value);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    x.Nombre.ToLower().Contains(f) ||
                    (x.Proveedor != null && x.Proveedor.Nombre.ToLower().Contains(f)) ||
                    (x.Categoria != null && x.Categoria.Nombre.ToLower().Contains(f)));
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery
                    .OrderBy(x => x.Proveedor != null ? x.Proveedor.Nombre : string.Empty)
                    .ThenBy(x => x.Nombre)
                    .ThenBy(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            return new PagedResultDto<ProductoNavDto>
            {
                Items = _mapper.Map<List<ProductoNavDto>>(entidades),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener productos de temporada");
            throw;
        }
    }

    public async Task<ProductoNavDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerConDetalleAsync(id);
            if (entidad == null || !entidad.IsActive) return null;
            return _mapper.Map<ProductoNavDto>(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener producto de temporada {Id}", id);
            throw;
        }
    }

    public async Task<ProductoNavDto> CrearAsync(CrearProductoNavDto dto)
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();

            var nombre = ValidarNombre(dto.Nombre);
            ValidarMontos(dto.PrecioCompraUnidad, dto.PrecioCatalogo);
            // En POST se ignora el Id de las presentaciones
            var presentaciones = NormalizarPresentaciones(dto.Presentaciones, ignorarIds: true);

            await ObtenerProveedorValidoAsync(dto.ProveedorId, temporada.Id);
            await ValidarCategoriaAsync(dto.CategoriaProductoId);

            if (await _repo.ExisteNombreAsync(dto.ProveedorId, nombre))
                throw new EntidadDuplicadaException("un producto para este proveedor", nombre);

            int productoId;
            await _uow.BeginTransactionAsync();
            try
            {
                var creado = await _repo.CrearAsync(new ProductoNav
                {
                    TemporadaId = temporada.Id,
                    ProveedorId = dto.ProveedorId,
                    CategoriaProductoId = dto.CategoriaProductoId,
                    Nombre = nombre,
                    PrecioCompraUnidad = dto.PrecioCompraUnidad,
                    PrecioCatalogo = dto.PrecioCatalogo
                });
                productoId = creado.Id;

                foreach (var p in presentaciones)
                    await _presentacionRepo.CrearAsync(NuevaPresentacion(productoId, p));

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Producto de temporada creado: {Id} (temporada {TemporadaId})", productoId, temporada.Id);

            return await ObtenerDetalleDtoAsync(productoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear producto de temporada");
            throw;
        }
    }

    public async Task<ProductoNavDto> ActualizarAsync(ActualizarProductoNavDto dto)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(dto.Id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Producto", dto.Id);

            await _temporadaActual.VerificarEditableAsync(existente.TemporadaId);

            var nombre = ValidarNombre(dto.Nombre);
            ValidarMontos(dto.PrecioCompraUnidad, dto.PrecioCatalogo);
            var presentaciones = NormalizarPresentaciones(dto.Presentaciones, ignorarIds: false);

            // El proveedor debe ser de la misma temporada que el producto
            await ObtenerProveedorValidoAsync(dto.ProveedorId, existente.TemporadaId);
            await ValidarCategoriaAsync(dto.CategoriaProductoId);

            if (await _repo.ExisteNombreAsync(dto.ProveedorId, nombre, existente.Id))
                throw new EntidadDuplicadaException("un producto para este proveedor", nombre);

            var activas = await _presentacionRepo.ObtenerActivasPorProductoAsync(existente.Id);
            var activasPorId = activas.ToDictionary(x => x.Id);

            // Cada Id recibido debe ser una presentación activa de este producto
            foreach (var p in presentaciones.Where(x => x.Id.HasValue))
            {
                if (!activasPorId.ContainsKey(p.Id!.Value))
                    throw new ValidacionException($"La presentación {p.Id.Value} no pertenece al producto");
            }

            var idsRecibidos = presentaciones.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();

            await _uow.BeginTransactionAsync();
            try
            {
                existente.ProveedorId = dto.ProveedorId;
                existente.CategoriaProductoId = dto.CategoriaProductoId;
                existente.Nombre = nombre;
                existente.PrecioCompraUnidad = dto.PrecioCompraUnidad;
                existente.PrecioCatalogo = dto.PrecioCatalogo;
                await _repo.ActualizarAsync(existente);

                // Orden para no chocar con el índice único filtrado (ProductoId, Unidades):
                // 1) desactivar las activas que no vienen
                foreach (var quitada in activas.Where(x => !idsRecibidos.Contains(x.Id)))
                    await _presentacionRepo.EliminarAsync(quitada.Id);

                // 2) actualizar las que traen Id
                foreach (var p in presentaciones.Where(x => x.Id.HasValue))
                {
                    var actual = activasPorId[p.Id!.Value];
                    actual.Nombre = p.Nombre!;
                    actual.Unidades = p.Unidades;
                    actual.PrecioUnitario = p.PrecioUnitario;
                    actual.EsPrincipal = p.EsPrincipal;
                    await _presentacionRepo.ActualizarAsync(actual);
                }

                // 3) crear las nuevas
                foreach (var p in presentaciones.Where(x => !x.Id.HasValue))
                    await _presentacionRepo.CrearAsync(NuevaPresentacion(existente.Id, p));

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Producto de temporada actualizado: {Id}", existente.Id);

            return await ObtenerDetalleDtoAsync(existente.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar producto de temporada {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Producto", id);

            await _temporadaActual.VerificarEditableAsync(existente.TemporadaId);

            // Borrado lógico solo del producto; las presentaciones quedan
            await _repo.EliminarAsync(id);
            _logger.LogInformation("Producto de temporada eliminado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar producto de temporada {Id}", id);
            throw;
        }
    }

    // Recarga con proveedor y presentaciones; el mapeo filtra activas y ordena por Unidades
    private async Task<ProductoNavDto> ObtenerDetalleDtoAsync(int id)
    {
        var entidad = await _repo.ObtenerConDetalleAsync(id)
            ?? throw new EntidadNoEncontradaException("Producto", id);
        return _mapper.Map<ProductoNavDto>(entidad);
    }

    // Proveedor activo y de la temporada indicada (sin tracking para no arrastrarlo en los Update)
    private async Task<ProveedorNav> ObtenerProveedorValidoAsync(int proveedorId, int temporadaId)
    {
        var proveedor = await _proveedorRepo.AsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == proveedorId);

        if (proveedor == null || !proveedor.IsActive)
            throw new ValidacionException("El proveedor no existe o está inactivo");
        if (proveedor.TemporadaId != temporadaId)
            throw new ValidacionException("El proveedor no pertenece a la temporada del producto");

        return proveedor;
    }

    // Categoría global: debe existir y estar activa
    private async Task ValidarCategoriaAsync(int categoriaProductoId)
    {
        var categoria = await _categoriaRepo.ObtenerPorIdAsync(categoriaProductoId);
        if (categoria == null || !categoria.IsActive)
            throw new ValidacionException("La categoría no existe o no está activa");
    }

    private static ProductoPresentacionNav NuevaPresentacion(int productoId, GuardarPresentacionNavDto p)
        => new()
        {
            ProductoId = productoId,
            Nombre = p.Nombre!,
            Unidades = p.Unidades,
            PrecioUnitario = p.PrecioUnitario,
            EsPrincipal = p.EsPrincipal
        };

    // Reglas de presentaciones (siempre en servicio, no solo en FluentValidation).
    // Devuelve copias normalizadas: nombre recortado y "Unidad" por defecto para la de 1 unidad.
    private static List<GuardarPresentacionNavDto> NormalizarPresentaciones(List<GuardarPresentacionNavDto>? entrada, bool ignorarIds)
    {
        if (entrada == null || entrada.Count == 0)
            throw new ValidacionException("Debe registrar al menos una presentación");

        if (entrada.Any(x => x == null))
            throw new ValidacionException("Hay presentaciones vacías en la solicitud");

        var lista = entrada.Select(x => new GuardarPresentacionNavDto
        {
            Id = ignorarIds ? null : x.Id,
            Nombre = x.Nombre?.Trim(),
            Unidades = x.Unidades,
            PrecioUnitario = x.PrecioUnitario,
            EsPrincipal = x.EsPrincipal
        }).ToList();

        if (lista.Any(x => x.Unidades < 1))
            throw new ValidacionException("Las unidades de cada presentación deben ser al menos 1");

        if (lista.Any(x => x.PrecioUnitario <= 0))
            throw new ValidacionException("El precio unitario de cada presentación debe ser mayor a 0");

        var repetidas = lista.GroupBy(x => x.Unidades).Where(g => g.Count() > 1).Select(g => g.Key).OrderBy(u => u).ToList();
        if (repetidas.Count > 0)
            throw new ValidacionException($"Unidades repetidas en las presentaciones: {string.Join(", ", repetidas)}");

        if (!lista.Any(x => x.Unidades == 1))
            throw new ValidacionException("Debe existir la presentación Unidad (1 unidad)");

        foreach (var p in lista)
        {
            if (string.IsNullOrWhiteSpace(p.Nombre))
            {
                if (p.Unidades == 1)
                    p.Nombre = NombreUnidadPorDefecto;
                else
                    throw new ValidacionException($"La presentación de {p.Unidades} unidades debe tener nombre");
            }

            if (p.Nombre!.Length > 50)
                throw new ValidacionException("El nombre de la presentación no puede superar 50 caracteres");
        }

        if (lista.Count(x => x.EsPrincipal) != 1)
            throw new ValidacionException("Debe haber exactamente una presentación principal");

        var idsRepetidos = lista.Where(x => x.Id.HasValue).GroupBy(x => x.Id!.Value).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (idsRepetidos.Count > 0)
            throw new ValidacionException($"La presentación {idsRepetidos[0]} está repetida en la solicitud");

        return lista;
    }

    // Reglas repetidas del validador por si FluentValidation no corre
    private static void ValidarMontos(decimal precioCompraUnidad, decimal precioCatalogo)
    {
        if (precioCompraUnidad <= 0)
            throw new ValidacionException("El precio de compra por unidad debe ser mayor a 0");
        if (precioCatalogo <= 0)
            throw new ValidacionException("El precio de catálogo debe ser mayor a 0");
    }

    private static string ValidarNombre(string? nombre)
    {
        var n = nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(n))
            throw new ValidacionException("El nombre es obligatorio");
        if (n.Length > 150)
            throw new ValidacionException("El nombre no puede superar 150 caracteres");
        return n;
    }
}
