using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Auth;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Auth;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Auth;

public class AlmacenService : IAlmacenService
{
    private readonly IAlmacenRepository _repositorio;
    private readonly IMapper _mapper;
    private readonly ILogger<AlmacenService> _logger;
    private readonly ICurrentUserService _currentUser;

    public AlmacenService(IAlmacenRepository repositorio, IMapper mapper, ILogger<AlmacenService> logger, ICurrentUserService currentUser)
    {
        _repositorio = repositorio;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<PagedResultDto<AlmacenDto>> ObtenerTodosAsync(GenericPaginationQueryDto query, int sistemaId = 1)
    {
        try
        {
            var baseQuery = _repositorio.AsQueryable().Where(a => a.SistemaId == sistemaId);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var filterLower = query.Filter.ToLower();
                baseQuery = baseQuery.Where(a => a.Nombre!.ToLower().Contains(filterLower));
            }

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query);

            var almacenes = await filteredQuery.ToListAsync();
            var dtos = _mapper.Map<List<AlmacenDto>>(almacenes);

            _logger.LogInformation("Se encontraron {Count} almacenes", dtos.Count);

            return new PagedResultDto<AlmacenDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener almacenes");
            throw;
        }
    }

    public async Task<AlmacenDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var almacen = await _repositorio.ObtenerPorIdAsync(id);
            return almacen == null ? null : _mapper.Map<AlmacenDto>(almacen);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener almacén {Id}", id);
            throw;
        }
    }

    public async Task<AlmacenDto> CrearAsync(CrearAlmacenDto dto)
    {
        try
        {
            var almacen = _mapper.Map<Almacen>(dto);
            almacen.SistemaId = dto.SistemaId ?? 1;
            almacen.CreatedAt = DateTime.UtcNow;
            var resultado = await _repositorio.CrearAsync(almacen);
            return _mapper.Map<AlmacenDto>(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear almacén");
            throw;
        }
    }

    // SistemaId no se modifica: el almacén conserva el sistema al que pertenece.
    public async Task<AlmacenDto> ActualizarAsync(ActualizarAlmacenDto dto)
    {
        try
        {
            var almacen = await _repositorio.ObtenerPorIdAsync(dto.Id)
                ?? throw new EntidadNoEncontradaException("Almacén", dto.Id);

            var nombre = dto.Nombre?.Trim() ?? string.Empty;
            var codigo = dto.Codigo?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 100)
                throw new ValidacionException("El nombre es obligatorio (máximo 100 caracteres)");
            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length > 20)
                throw new ValidacionException("El código es obligatorio (máximo 20 caracteres)");

            // El índice único de Codigo cubre también a los inactivos
            var codigoEnUso = await _repositorio.AsQueryable()
                .AnyAsync(a => a.Codigo == codigo && a.Id != dto.Id);
            if (codigoEnUso)
                throw new ExcepcionDominio($"Ya existe un almacén con el código '{codigo}'", 409, "ENTIDAD_DUPLICADA");

            almacen.Nombre = nombre;
            almacen.Codigo = codigo;
            almacen.EsTienda = dto.EsTienda;
            almacen.Direccion = string.IsNullOrWhiteSpace(dto.Direccion) ? null : dto.Direccion.Trim();
            almacen.IsActive = dto.IsActive;
            almacen.UpdatedAt = DateTime.UtcNow;
            almacen.UpdatedByUsuarioId = _currentUser.GetUsuarioId() ?? 1;

            var resultado = await _repositorio.ActualizarAsync(almacen);
            return _mapper.Map<AlmacenDto>(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar almacén {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            _ = await _repositorio.ObtenerPorIdAsync(id)
                ?? throw new EntidadNoEncontradaException("Almacén", id);

            // Borrado lógico (IsActive=false, DeletedAt, DeletedByUsuarioId) vía RepositorioBase
            await _repositorio.EliminarAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar almacén {Id}", id);
            throw;
        }
    }
}
