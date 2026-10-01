using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Helpers;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Sistema;

// Nota: la conversión de CreatedAt a hora de Bolivia se hace en MappingProfile
// (CreateMap<CategoriaGasto, CategoriaGastoDto>().ForMember(CreatedAt, BoliviaTimeZone.ToLocal)), igual que Cliente.
// No se vuelve a convertir aquí para evitar doble conversión.
public class CategoriaGastoService : ICategoriaGastoService
{
    private readonly ICategoriaGastoRepository _repo;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoriaGastoService> _logger;

    public CategoriaGastoService(
        ICategoriaGastoRepository repo,
        IMapper mapper,
        ILogger<CategoriaGastoService> logger)
    {
        _repo = repo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<CategoriaGastoDto>> ObtenerTodosAsync(GenericPaginationQueryDto query)
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

            return new PagedResultDto<CategoriaGastoDto>
            {
                Items = _mapper.Map<List<CategoriaGastoDto>>(entidades),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener categorías de gasto");
            throw;
        }
    }

    public async Task<CategoriaGastoDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerPorIdAsync(id);
            if (entidad == null || !entidad.IsActive) return null;

            return _mapper.Map<CategoriaGastoDto>(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener categoría de gasto {Id}", id);
            throw;
        }
    }

    public async Task<CategoriaGastoDto> CrearAsync(CrearCategoriaGastoDto dto)
    {
        try
        {
            dto.Nombre = dto.Nombre?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new ValidacionException("El nombre es obligatorio");

            ValidacionEntidad.VerificarNoDuplicado(
                await _repo.ExisteNombreAsync(dto.Nombre),
                "categoría de gasto", dto.Nombre);

            var entidad = _mapper.Map<CategoriaGasto>(dto);
            var creada = await _repo.CrearAsync(entidad);

            _logger.LogInformation("Categoría de gasto creada: {Nombre}", creada.Nombre);

            return _mapper.Map<CategoriaGastoDto>(creada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear categoría de gasto");
            throw;
        }
    }

    public async Task<CategoriaGastoDto> ActualizarAsync(ActualizarCategoriaGastoDto dto)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(dto.Id);
            ValidacionEntidad.VerificarActivo(existente, "Categoría de gasto");

            dto.Nombre = dto.Nombre?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new ValidacionException("El nombre es obligatorio");

            ValidacionEntidad.VerificarNoDuplicado(
                await _repo.ExisteNombreAsync(dto.Nombre, dto.Id),
                "categoría de gasto", dto.Nombre);

            existente!.Nombre = dto.Nombre;
            existente.IsActive = dto.IsActive;

            var actualizada = await _repo.ActualizarAsync(existente);

            _logger.LogInformation("Categoría de gasto actualizada: {Nombre}", actualizada.Nombre);

            return _mapper.Map<CategoriaGastoDto>(actualizada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar categoría de gasto {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            ValidacionEntidad.VerificarActivo(existente, "Categoría de gasto");

            await _repo.EliminarAsync(id);
            _logger.LogInformation("Categoría de gasto eliminada: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar categoría de gasto {Id}", id);
            throw;
        }
    }
}
