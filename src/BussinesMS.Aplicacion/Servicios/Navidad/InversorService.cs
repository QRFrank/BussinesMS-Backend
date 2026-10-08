using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Nota: CreatedAt se convierte a hora de Bolivia en NavidadMappingProfile (no reconvertir aquí).
public class InversorService : IInversorService
{
    private readonly IInversorRepository _repo;
    private readonly IMapper _mapper;
    private readonly ILogger<InversorService> _logger;

    public InversorService(
        IInversorRepository repo,
        IMapper mapper,
        ILogger<InversorService> logger)
    {
        _repo = repo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<InversorDto>> ObtenerTodosAsync(GenericPaginationQueryDto query)
    {
        try
        {
            var baseQuery = _repo.AsQueryable().Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    x.Nombre.ToLower().Contains(f) ||
                    (x.Documento != null && x.Documento.ToLower().Contains(f)) ||
                    (x.Telefono != null && x.Telefono.ToLower().Contains(f)));
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderBy(x => x.Nombre).ThenBy(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            return new PagedResultDto<InversorDto>
            {
                Items = _mapper.Map<List<InversorDto>>(entidades),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener inversores");
            throw;
        }
    }

    public async Task<InversorDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerPorIdAsync(id);
            if (entidad == null || !entidad.IsActive) return null;
            return _mapper.Map<InversorDto>(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener inversor {Id}", id);
            throw;
        }
    }

    public async Task<InversorDto> CrearAsync(CrearInversorDto dto)
    {
        try
        {
            var nombre = dto.Nombre?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ValidacionException("El nombre es obligatorio");

            var entidad = _mapper.Map<Inversor>(dto);
            entidad.Nombre = nombre;
            entidad.Documento = Limpiar(dto.Documento);
            entidad.Telefono = Limpiar(dto.Telefono);

            var creada = await _repo.CrearAsync(entidad);

            _logger.LogInformation("Inversor creado: {Nombre}", creada.Nombre);

            return _mapper.Map<InversorDto>(creada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear inversor");
            throw;
        }
    }

    public async Task<InversorDto> ActualizarAsync(ActualizarInversorDto dto)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(dto.Id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Inversor", dto.Id);

            var nombre = dto.Nombre?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ValidacionException("El nombre es obligatorio");

            // Desactivar por PUT equivale a eliminar: misma regla
            if (!dto.IsActive && await _repo.TieneAportesEnTemporadaAbiertaAsync(dto.Id))
                throw new ValidacionException("El inversor tiene aportes en la temporada abierta");

            existente.Nombre = nombre;
            existente.Documento = Limpiar(dto.Documento);
            existente.Telefono = Limpiar(dto.Telefono);
            existente.IsActive = dto.IsActive;

            var actualizada = await _repo.ActualizarAsync(existente);

            _logger.LogInformation("Inversor actualizado: {Id}", actualizada.Id);

            return _mapper.Map<InversorDto>(actualizada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar inversor {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Inversor", id);

            if (await _repo.TieneAportesEnTemporadaAbiertaAsync(id))
                throw new ValidacionException("El inversor tiene aportes en la temporada abierta");

            await _repo.EliminarAsync(id);
            _logger.LogInformation("Inversor eliminado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar inversor {Id}", id);
            throw;
        }
    }

    private static string? Limpiar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
