using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Helpers;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Nota: CreatedAt se convierte a hora de Bolivia en NavidadMappingProfile (no reconvertir aquí).
public class CategoriaProductoNavService : ICategoriaProductoNavService
{
    private readonly ICategoriaProductoNavRepository _repo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoriaProductoNavService> _logger;

    public CategoriaProductoNavService(
        ICategoriaProductoNavRepository repo,
        IProductoNavRepository productoRepo,
        IMapper mapper,
        ILogger<CategoriaProductoNavService> logger)
    {
        _repo = repo;
        _productoRepo = productoRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<CategoriaProductoNavDto>> ObtenerTodosAsync(GenericPaginationQueryDto query)
    {
        try
        {
            var baseQuery = _repo.AsQueryable().Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x => x.Nombre.ToLower().Contains(f));
            }

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query);
            var entidades = await filteredQuery.ToListAsync();

            return new PagedResultDto<CategoriaProductoNavDto>
            {
                Items = _mapper.Map<List<CategoriaProductoNavDto>>(entidades),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener categorías de producto (Navidad)");
            throw;
        }
    }

    public async Task<CategoriaProductoNavDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerPorIdAsync(id);
            if (entidad == null || !entidad.IsActive) return null;

            return _mapper.Map<CategoriaProductoNavDto>(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener categoría de producto (Navidad) {Id}", id);
            throw;
        }
    }

    public async Task<CategoriaProductoNavDto> CrearAsync(CrearCategoriaProductoNavDto dto)
    {
        try
        {
            dto.Nombre = dto.Nombre?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new ValidacionException("El nombre es obligatorio");

            ValidacionEntidad.VerificarNoDuplicado(
                await _repo.ExisteNombreAsync(dto.Nombre),
                "categoría de producto", dto.Nombre);

            var entidad = _mapper.Map<CategoriaProductoNav>(dto);
            var creada = await _repo.CrearAsync(entidad);

            _logger.LogInformation("Categoría de producto (Navidad) creada: {Nombre}", creada.Nombre);

            return _mapper.Map<CategoriaProductoNavDto>(creada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear categoría de producto (Navidad)");
            throw;
        }
    }

    public async Task<CategoriaProductoNavDto> ActualizarAsync(ActualizarCategoriaProductoNavDto dto)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(dto.Id);
            ValidacionEntidad.VerificarActivo(existente, "Categoría de producto");

            dto.Nombre = dto.Nombre?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new ValidacionException("El nombre es obligatorio");

            ValidacionEntidad.VerificarNoDuplicado(
                await _repo.ExisteNombreAsync(dto.Nombre, dto.Id),
                "categoría de producto", dto.Nombre);

            existente!.Nombre = dto.Nombre;
            existente.IsActive = dto.IsActive;

            var actualizada = await _repo.ActualizarAsync(existente);

            _logger.LogInformation("Categoría de producto (Navidad) actualizada: {Nombre}", actualizada.Nombre);

            return _mapper.Map<CategoriaProductoNavDto>(actualizada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar categoría de producto (Navidad) {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            ValidacionEntidad.VerificarActivo(existente, "Categoría de producto");

            if (await _productoRepo.TieneProductosActivosPorCategoriaAsync(id))
                throw new ValidacionException("La categoría tiene productos registrados");

            await _repo.EliminarAsync(id);
            _logger.LogInformation("Categoría de producto (Navidad) eliminada: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar categoría de producto (Navidad) {Id}", id);
            throw;
        }
    }
}
