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
public class CategoriaGastoNavService : ICategoriaGastoNavService
{
    private readonly ICategoriaGastoNavRepository _repo;
    private readonly IGastoNavRepository _gastoRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoriaGastoNavService> _logger;

    public CategoriaGastoNavService(
        ICategoriaGastoNavRepository repo,
        IGastoNavRepository gastoRepo,
        IMapper mapper,
        ILogger<CategoriaGastoNavService> logger)
    {
        _repo = repo;
        _gastoRepo = gastoRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<CategoriaGastoNavDto>> ObtenerTodosAsync(GenericPaginationQueryDto query)
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

            return new PagedResultDto<CategoriaGastoNavDto>
            {
                Items = _mapper.Map<List<CategoriaGastoNavDto>>(entidades),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener categorías de gasto (Navidad)");
            throw;
        }
    }

    public async Task<CategoriaGastoNavDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerPorIdAsync(id);
            if (entidad == null || !entidad.IsActive) return null;

            return _mapper.Map<CategoriaGastoNavDto>(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener categoría de gasto (Navidad) {Id}", id);
            throw;
        }
    }

    public async Task<CategoriaGastoNavDto> CrearAsync(CrearCategoriaGastoNavDto dto)
    {
        try
        {
            dto.Nombre = dto.Nombre?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new ValidacionException("El nombre es obligatorio");

            ValidacionEntidad.VerificarNoDuplicado(
                await _repo.ExisteNombreAsync(dto.Nombre),
                "categoría de gasto", dto.Nombre);

            var entidad = _mapper.Map<CategoriaGastoNav>(dto);
            var creada = await _repo.CrearAsync(entidad);

            _logger.LogInformation("Categoría de gasto (Navidad) creada: {Nombre}", creada.Nombre);

            return _mapper.Map<CategoriaGastoNavDto>(creada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear categoría de gasto (Navidad)");
            throw;
        }
    }

    public async Task<CategoriaGastoNavDto> ActualizarAsync(ActualizarCategoriaGastoNavDto dto)
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

            _logger.LogInformation("Categoría de gasto (Navidad) actualizada: {Nombre}", actualizada.Nombre);

            return _mapper.Map<CategoriaGastoNavDto>(actualizada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar categoría de gasto (Navidad) {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            ValidacionEntidad.VerificarActivo(existente, "Categoría de gasto");

            if (await _gastoRepo.TieneGastosActivosPorCategoriaAsync(id))
                throw new ValidacionException("La categoría tiene gastos registrados");

            await _repo.EliminarAsync(id);
            _logger.LogInformation("Categoría de gasto (Navidad) eliminada: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar categoría de gasto (Navidad) {Id}", id);
            throw;
        }
    }
}
