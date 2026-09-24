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
// (CreateMap<Cliente, ClienteDto>().ForMember(CreatedAt, BoliviaTimeZone.ToLocal)), igual que Proveedor.
// No se vuelve a convertir aquí para evitar doble conversión.
public class ClienteService : IClienteService
{
    private const string MensajeClienteGenerico = "El cliente genérico no puede modificarse ni eliminarse";

    private readonly IClienteRepository _repo;
    private readonly IMapper _mapper;
    private readonly ILogger<ClienteService> _logger;

    public ClienteService(
        IClienteRepository repo,
        IMapper mapper,
        ILogger<ClienteService> logger)
    {
        _repo = repo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<ClienteDto>> ObtenerTodosAsync(GenericPaginationQueryDto query)
    {
        try
        {
            var baseQuery = _repo.AsQueryable().Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    x.Nombre.ToLower().Contains(f) ||
                    (x.NumeroCarnet != null && x.NumeroCarnet.ToLower().Contains(f)) ||
                    (x.Telefono != null && x.Telefono.ToLower().Contains(f)));
            }

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query);
            var entidades = await filteredQuery.ToListAsync();

            return new PagedResultDto<ClienteDto>
            {
                Items = _mapper.Map<List<ClienteDto>>(entidades),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener clientes");
            throw;
        }
    }

    public async Task<ClienteDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerPorIdAsync(id);
            if (entidad == null || !entidad.IsActive) return null;

            return _mapper.Map<ClienteDto>(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cliente {Id}", id);
            throw;
        }
    }

    public async Task<ClienteDto?> ObtenerPorNumeroCarnetAsync(string numeroCarnet)
    {
        try
        {
            var carnet = Normalizar(numeroCarnet);
            if (carnet == null) return null;

            var entidad = await _repo.ObtenerPorNumeroCarnetAsync(carnet);
            if (entidad == null || !entidad.IsActive) return null;

            return _mapper.Map<ClienteDto>(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cliente por carnet {NumeroCarnet}", numeroCarnet);
            throw;
        }
    }

    public async Task<(ClienteDto Entidad, bool FueReactivada)> CrearAsync(CrearClienteDto dto)
    {
        try
        {
            dto.NumeroCarnet = Normalizar(dto.NumeroCarnet);
            dto.Telefono = Normalizar(dto.Telefono);

            if (dto.NumeroCarnet != null)
            {
                var duplicado = await _repo.ObtenerPorNumeroCarnetAsync(dto.NumeroCarnet);

                if (duplicado != null)
                {
                    if (duplicado.IsActive)
                        throw new ValidacionException(
                            $"Ya existe un cliente con el número de carnet '{dto.NumeroCarnet}'");

                    var reactivada = await _repo.ReactivarAsync(duplicado.Id);
                    _logger.LogInformation("Cliente reactivado: {Nombre}", reactivada.Nombre);
                    return (_mapper.Map<ClienteDto>(reactivada), true);
                }
            }

            var entidad = _mapper.Map<Cliente>(dto);
            var creada = await _repo.CrearAsync(entidad);

            _logger.LogInformation("Cliente creado: {Nombre}", creada.Nombre);

            return (_mapper.Map<ClienteDto>(creada), false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear cliente");
            throw;
        }
    }

    public async Task<ClienteDto> ActualizarAsync(ActualizarClienteDto dto)
    {
        try
        {
            if (dto.Id == Cliente.ClienteGenericoId)
                throw new ValidacionException(MensajeClienteGenerico);

            var existente = await _repo.ObtenerPorIdAsync(dto.Id);
            ValidacionEntidad.VerificarActivo(existente, "Cliente");

            dto.NumeroCarnet = Normalizar(dto.NumeroCarnet);
            dto.Telefono = Normalizar(dto.Telefono);

            if (dto.NumeroCarnet != null &&
                await _repo.ExisteNumeroCarnetAsync(dto.NumeroCarnet, dto.Id))
                throw new ValidacionException(
                    $"Ya existe un cliente con el número de carnet '{dto.NumeroCarnet}'");

            existente!.Nombre = dto.Nombre;
            existente.NumeroCarnet = dto.NumeroCarnet;
            existente.Telefono = dto.Telefono;
            existente.IsActive = dto.IsActive;

            var actualizada = await _repo.ActualizarAsync(existente);

            _logger.LogInformation("Cliente actualizado: {Nombre}", actualizada.Nombre);

            return _mapper.Map<ClienteDto>(actualizada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar cliente {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            if (id == Cliente.ClienteGenericoId)
                throw new ValidacionException(MensajeClienteGenerico);

            var existente = await _repo.ObtenerPorIdAsync(id);
            ValidacionEntidad.VerificarActivo(existente, "Cliente");

            await _repo.EliminarAsync(id);
            _logger.LogInformation("Cliente eliminado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar cliente {Id}", id);
            throw;
        }
    }

    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
