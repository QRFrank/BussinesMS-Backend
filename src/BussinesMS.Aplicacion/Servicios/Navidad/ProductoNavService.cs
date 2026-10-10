using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductoNav = BussinesMS.Dominio.Entidades.Navidad.Producto;
using ProveedorNav = BussinesMS.Dominio.Entidades.Navidad.Proveedor;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Nota: CreatedAt se convierte a hora de Bolivia en NavidadMappingProfile (no reconvertir aquí).
public class ProductoNavService : IProductoNavService
{
    private readonly IProductoNavRepository _repo;
    private readonly IProveedorNavRepository _proveedorRepo;
    private readonly ICategoriaProductoNavRepository _categoriaRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductoNavService> _logger;

    public ProductoNavService(
        IProductoNavRepository repo,
        IProveedorNavRepository proveedorRepo,
        ICategoriaProductoNavRepository categoriaRepo,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        IMapper mapper,
        ILogger<ProductoNavService> logger)
    {
        _repo = repo;
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
                .Where(x => x.TemporadaId == temporadaId);

            // isActive manda; si no viene, incluirInactivos=true trae todos; por defecto solo activos
            if (query.IsActive.HasValue)
                baseQuery = baseQuery.Where(x => x.IsActive == query.IsActive.Value);
            else if (query.IncluirInactivos != true)
                baseQuery = baseQuery.Where(x => x.IsActive);

            if (query.ProveedorId.HasValue)
                baseQuery = baseQuery.Where(x => x.ProveedorId == query.ProveedorId.Value);

            if (query.CategoriaProductoId.HasValue)
                baseQuery = baseQuery.Where(x => x.CategoriaProductoId == query.CategoriaProductoId.Value);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    x.Descripcion.ToLower().Contains(f) ||
                    (x.Nombre != null && x.Nombre.ToLower().Contains(f)) ||
                    (x.Proveedor != null && x.Proveedor.Nombre.ToLower().Contains(f)) ||
                    (x.Categoria != null && x.Categoria.Nombre.ToLower().Contains(f)));
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery
                    .OrderBy(x => x.Nombre ?? x.Descripcion) // nombreMostrar
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
            if (entidad == null) return null;
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

            var descripcion = ValidarDescripcion(dto.Descripcion);
            var nombre = NormalizarNombre(dto.Nombre);
            ValidarMontos(dto.PrecioCompraUnidad, dto.PrecioCatalogo);
            var (unidadesPorEmpaque, nombreEmpaque) = NormalizarEmpaque(dto.UnidadesPorEmpaque, dto.NombreEmpaque);
            var color = NormalizarColor(dto.Color);

            await ObtenerProveedorValidoAsync(dto.ProveedorId, temporada.Id);
            await ValidarCategoriaAsync(dto.CategoriaProductoId);

            await ValidarUnicidadAsync(dto.ProveedorId, temporada.Id, descripcion, nombre, excluirId: null);

            var creado = await _repo.CrearAsync(new ProductoNav
            {
                TemporadaId = temporada.Id,
                ProveedorId = dto.ProveedorId,
                CategoriaProductoId = dto.CategoriaProductoId,
                Descripcion = descripcion,
                Nombre = nombre,
                PrecioCompraUnidad = dto.PrecioCompraUnidad,
                PrecioCatalogo = dto.PrecioCatalogo,
                UnidadesPorEmpaque = unidadesPorEmpaque,
                NombreEmpaque = nombreEmpaque,
                Color = color
            });
            var productoId = creado.Id;

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

            var descripcion = ValidarDescripcion(dto.Descripcion);
            var nombre = NormalizarNombre(dto.Nombre);
            ValidarMontos(dto.PrecioCompraUnidad, dto.PrecioCatalogo);
            var (unidadesPorEmpaque, nombreEmpaque) = NormalizarEmpaque(dto.UnidadesPorEmpaque, dto.NombreEmpaque);
            var color = NormalizarColor(dto.Color);

            // El proveedor debe ser de la misma temporada que el producto
            await ObtenerProveedorValidoAsync(dto.ProveedorId, existente.TemporadaId);
            await ValidarCategoriaAsync(dto.CategoriaProductoId);

            await ValidarUnicidadAsync(dto.ProveedorId, existente.TemporadaId, descripcion, nombre, existente.Id);

            existente.ProveedorId = dto.ProveedorId;
            existente.CategoriaProductoId = dto.CategoriaProductoId;
            existente.Descripcion = descripcion;
            existente.Nombre = nombre;
            existente.PrecioCompraUnidad = dto.PrecioCompraUnidad;
            existente.PrecioCatalogo = dto.PrecioCatalogo;
            existente.UnidadesPorEmpaque = unidadesPorEmpaque;
            existente.NombreEmpaque = nombreEmpaque;
            existente.Color = color;
            await _repo.ActualizarAsync(existente);

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

            // Borrado lógico
            await _repo.EliminarAsync(id);
            _logger.LogInformation("Producto de temporada eliminado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar producto de temporada {Id}", id);
            throw;
        }
    }

    // Activa o desactiva un producto (PATCH /{id}/estado). Idempotente si ya está en ese estado.
    public async Task<ProductoNavDto> CambiarEstadoAsync(int id, bool isActive)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            if (existente == null)
                throw new EntidadNoEncontradaException("Producto", id);

            await _temporadaActual.VerificarEditableAsync(existente.TemporadaId);

            if (existente.IsActive != isActive)
            {
                // Al activar pueden chocar los índices únicos filtrados por IsActive
                if (isActive)
                {
                    if (await _repo.ExisteDescripcionAsync(existente.ProveedorId, existente.Descripcion, existente.Id))
                        throw new ExcepcionDominio(
                            $"No se puede activar: ya existe un producto activo para este proveedor con la descripción '{existente.Descripcion}'",
                            409, "ENTIDAD_DUPLICADA");

                    if (existente.Nombre != null && await _repo.ExisteNombreEnTemporadaAsync(existente.TemporadaId, existente.Nombre, existente.Id))
                        throw new ExcepcionDominio(
                            $"No se puede activar: ya existe un producto activo en esta temporada con el nombre '{existente.Nombre}'",
                            409, "ENTIDAD_DUPLICADA");
                }

                await _repo.CambiarEstadoAsync(existente, isActive);
                _logger.LogInformation("Producto de temporada {Id} {Estado}", id, isActive ? "activado" : "desactivado");
            }

            return await ObtenerDetalleDtoAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar el estado del producto de temporada {Id}", id);
            throw;
        }
    }

    // Actualización masiva de precios de productos de la temporada abierta (todo o nada)
    public async Task<ActualizarPreciosResultadoNavDto> ActualizarPreciosAsync(List<ActualizarPrecioProductoNavDto> items)
    {
        try
        {
            if (items == null || items.Count == 0)
                throw new ValidacionException("Debe enviar al menos un producto");

            if (items.Select(i => i.Id).Distinct().Count() != items.Count)
                throw new ValidacionException("Hay productos repetidos en la lista");

            foreach (var item in items)
                ValidarMontos(item.PrecioCompraUnidad, item.PrecioCatalogo);

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();

            var ids = items.Select(i => i.Id).ToList();
            var productos = await _repo.AsQueryable()
                .Where(p => ids.Contains(p.Id) && p.TemporadaId == temporada.Id)
                .ToListAsync();

            var invalidos = ids.Except(productos.Select(p => p.Id)).ToList();
            if (invalidos.Count > 0)
                throw new ValidacionException(
                    $"Los productos {string.Join(", ", invalidos)} no existen o no son de la temporada abierta");

            // Un producto inactivo no se puede usar: se rechaza todo el lote
            var inactivos = productos.Where(p => !p.IsActive).Select(p => p.Nombre ?? p.Descripcion).ToList();
            if (inactivos.Count > 0)
                throw new ValidacionException(inactivos.Count == 1
                    ? $"El producto {inactivos[0]} está inactivo"
                    : $"Los productos {string.Join(", ", inactivos)} están inactivos");

            var porId = productos.ToDictionary(p => p.Id);

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var item in items)
                {
                    var producto = porId[item.Id];
                    producto.PrecioCompraUnidad = item.PrecioCompraUnidad;
                    producto.PrecioCatalogo = item.PrecioCatalogo;
                    await _repo.ActualizarAsync(producto);
                }

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Precios actualizados para {Cantidad} productos (temporada {TemporadaId})", items.Count, temporada.Id);

            return new ActualizarPreciosResultadoNavDto { Actualizados = items.Count };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar precios de productos de temporada");
            throw;
        }
    }

    // Recarga con proveedor y categoría
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

    // Empaque opcional (siempre en servicio, no solo en FluentValidation):
    // NombreEmpaque recortado y vacío pasa a null; ambos van juntos; UnidadesPorEmpaque > 1.
    private static (int? Unidades, string? Nombre) NormalizarEmpaque(int? unidadesPorEmpaque, string? nombreEmpaque)
    {
        var nombre = nombreEmpaque?.Trim();
        if (string.IsNullOrEmpty(nombre))
            nombre = null;

        if (unidadesPorEmpaque.HasValue != (nombre != null))
            throw new ValidacionException("Las unidades por empaque y el nombre del empaque van juntos");

        if (unidadesPorEmpaque.HasValue && unidadesPorEmpaque.Value <= 1)
            throw new ValidacionException("Las unidades por empaque deben ser mayores a 1");

        if (nombre != null && nombre.Length > 20)
            throw new ValidacionException("El nombre del empaque no puede superar 20 caracteres");

        return (unidadesPorEmpaque, nombre);
    }

    // Color opcional: vacío o solo espacios pasa a null; si viene, hex #RRGGBB
    private static string? NormalizarColor(string? color)
    {
        var c = color?.Trim();
        if (string.IsNullOrEmpty(c))
            return null;
        if (!System.Text.RegularExpressions.Regex.IsMatch(c, "^#[0-9A-Fa-f]{6}$"))
            throw new ValidacionException("El color debe tener formato hexadecimal #RRGGBB");
        return c;
    }

    // Reglas repetidas del validador por si FluentValidation no corre
    private static void ValidarMontos(decimal precioCompraUnidad, decimal precioCatalogo)
    {
        if (precioCompraUnidad < 0)
            throw new ValidacionException("El precio de compra por unidad no puede ser negativo");
        if (precioCatalogo < 0)
            throw new ValidacionException("El precio de catálogo no puede ser negativo");
    }

    private static string ValidarDescripcion(string? descripcion)
    {
        var d = descripcion?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(d))
            throw new ValidacionException("La descripción es obligatoria");
        if (d.Length > 150)
            throw new ValidacionException("La descripción no puede superar 150 caracteres");
        return d;
    }

    // Alias opcional: vacío o solo espacios se guarda como null
    private static string? NormalizarNombre(string? nombre)
    {
        var n = nombre?.Trim();
        if (string.IsNullOrEmpty(n))
            return null;
        if (n.Length > 150)
            throw new ValidacionException("El nombre no puede superar 150 caracteres");
        return n;
    }

    // Descripción única por proveedor y alias único por temporada (entre activos, sin distinguir mayúsculas)
    private async Task ValidarUnicidadAsync(int proveedorId, int temporadaId, string descripcion, string? nombre, int? excluirId)
    {
        if (await _repo.ExisteDescripcionAsync(proveedorId, descripcion, excluirId))
            throw new ExcepcionDominio(
                $"Ya existe un producto para este proveedor con la descripción '{descripcion}'", 409, "ENTIDAD_DUPLICADA");

        if (nombre != null && await _repo.ExisteNombreEnTemporadaAsync(temporadaId, nombre, excluirId))
            throw new EntidadDuplicadaException("un producto en esta temporada", nombre);
    }
}
