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
public class ClienteNavService : IClienteNavService
{
    private readonly IClienteNavRepository _repo;
    private readonly IMapper _mapper;
    private readonly ILogger<ClienteNavService> _logger;

    public ClienteNavService(
        IClienteNavRepository repo,
        IMapper mapper,
        ILogger<ClienteNavService> logger)
    {
        _repo = repo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<ClienteNavDto>> ObtenerTodosAsync(GenericPaginationQueryDto query)
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

            return new PagedResultDto<ClienteNavDto>
            {
                Items = _mapper.Map<List<ClienteNavDto>>(entidades),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener clientes navideños");
            throw;
        }
    }

    public async Task<ClienteNavDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerPorIdAsync(id);
            if (entidad == null || !entidad.IsActive) return null;
            return _mapper.Map<ClienteNavDto>(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cliente navideño {Id}", id);
            throw;
        }
    }

    public async Task<ClienteNavDto> CrearAsync(CrearClienteNavDto dto)
    {
        try
        {
            var nombre = dto.Nombre?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ValidacionException("El nombre es obligatorio");

            var documento = Limpiar(dto.Documento);
            await ValidarDocumentoUnicoAsync(documento, null);

            var entidad = _mapper.Map<ClienteNav>(dto);
            entidad.Nombre = nombre;
            entidad.Documento = documento;
            entidad.Telefono = Limpiar(dto.Telefono);
            entidad.Direccion = Limpiar(dto.Direccion);

            var creada = await _repo.CrearAsync(entidad);

            _logger.LogInformation("Cliente navideño creado: {Nombre}", creada.Nombre);

            return _mapper.Map<ClienteNavDto>(creada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear cliente navideño");
            throw;
        }
    }

    public async Task<ClienteNavDto> ActualizarAsync(ActualizarClienteNavDto dto)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(dto.Id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Cliente", dto.Id);

            var nombre = dto.Nombre?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ValidacionException("El nombre es obligatorio");

            var documento = Limpiar(dto.Documento);
            // Si queda inactivo no choca con el índice único filtrado por activos
            if (dto.IsActive)
                await ValidarDocumentoUnicoAsync(documento, existente.Id);

            existente.Nombre = nombre;
            existente.Documento = documento;
            existente.Telefono = Limpiar(dto.Telefono);
            existente.Direccion = Limpiar(dto.Direccion);
            existente.IsActive = dto.IsActive;

            var actualizada = await _repo.ActualizarAsync(existente);

            _logger.LogInformation("Cliente navideño actualizado: {Id}", actualizada.Id);

            return _mapper.Map<ClienteNavDto>(actualizada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar cliente navideño {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Cliente", id);

            // Sin restricciones por ahora: las ventas navideñas aún no existen
            await _repo.EliminarAsync(id);
            _logger.LogInformation("Cliente navideño eliminado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar cliente navideño {Id}", id);
            throw;
        }
    }

    private async Task ValidarDocumentoUnicoAsync(string? documento, int? excluirId)
    {
        if (documento == null) return;
        if (await _repo.ExisteDocumentoAsync(documento, excluirId))
            throw new ExcepcionDominio($"Ya existe un cliente con el documento '{documento}'", 409, "ENTIDAD_DUPLICADA");
    }

    private static string? Limpiar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
