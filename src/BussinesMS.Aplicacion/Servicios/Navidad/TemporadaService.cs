using AutoMapper;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Nota: CreatedAt se convierte a hora de Bolivia en NavidadMappingProfile (no reconvertir aquí).
public class TemporadaService : ITemporadaService
{
    private const int SistemaNavidadId = 2;

    private readonly ITemporadaRepository _repo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly INavidadUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<TemporadaService> _logger;

    public TemporadaService(
        ITemporadaRepository repo,
        ITemporadaActualService temporadaActual,
        IAlmacenRepository almacenRepo,
        INavidadUnitOfWork uow,
        IMapper mapper,
        ILogger<TemporadaService> logger)
    {
        _repo = repo;
        _temporadaActual = temporadaActual;
        _almacenRepo = almacenRepo;
        _uow = uow;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<TemporadaDto>> ObtenerTodosAsync(GenericPaginationQueryDto query)
    {
        try
        {
            var baseQuery = _repo.AsQueryable().Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x => x.Nombre.ToLower().Contains(f) || x.Anio.ToString().Contains(f));
            }

            // Orden por defecto: año descendente
            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(x => x.Anio).ThenByDescending(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            var items = _mapper.Map<List<TemporadaDto>>(entidades);
            await CompletarAlmacenesConteoAsync(items);

            return new PagedResultDto<TemporadaDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener temporadas");
            throw;
        }
    }

    public async Task<TemporadaDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerPorIdAsync(id);
            if (entidad == null || !entidad.IsActive) return null;

            return await MapearAsync(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener temporada {Id}", id);
            throw;
        }
    }

    public async Task<TemporadaDto> ObtenerActualAsync()
    {
        try
        {
            var abierta = await _temporadaActual.ObtenerAbiertaAsync();
            return await MapearAsync(abierta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la temporada actual");
            throw;
        }
    }

    public async Task<TemporadaDto> CrearAsync(CrearTemporadaDto dto)
    {
        try
        {
            if (await _repo.ExisteAbiertaAsync())
                throw new ExcepcionDominio("Ya existe una temporada abierta. Ciérrela antes de crear otra.", 409, "TEMPORADA_ABIERTA_EXISTE");

            dto.Nombre = dto.Nombre?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new ValidacionException("El nombre es obligatorio");

            await ValidarAlmacenesConteoAsync(dto.AlmacenesConConteoIds);

            var entidad = _mapper.Map<Temporada>(dto);
            entidad.FechaInicio = dto.FechaInicio.Date;
            entidad.FechaCierre = null;
            entidad.Estado = EstadoTemporada.Abierta;

            Temporada creada;
            await _uow.BeginTransactionAsync();
            try
            {
                creada = await _repo.CrearAsync(entidad);

                if (dto.AlmacenesConConteoIds != null && dto.AlmacenesConConteoIds.Count > 0)
                    await _repo.ReemplazarAlmacenesConteoAsync(creada.Id, dto.AlmacenesConConteoIds);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Temporada creada: {Nombre} ({Anio})", creada.Nombre, creada.Anio);

            return await MapearAsync(creada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear temporada");
            throw;
        }
    }

    public async Task<TemporadaDto> ActualizarAsync(ActualizarTemporadaDto dto)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(dto.Id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Temporada", dto.Id);

            _temporadaActual.VerificarEditable(existente);

            dto.Nombre = dto.Nombre?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new ValidacionException("El nombre es obligatorio");

            await ValidarAlmacenesConteoAsync(dto.AlmacenesConConteoIds);

            // El Estado no se modifica aquí (solo vía CerrarAsync)
            existente.Anio = dto.Anio;
            existente.Nombre = dto.Nombre;
            existente.FechaInicio = dto.FechaInicio.Date;

            Temporada actualizada;
            await _uow.BeginTransactionAsync();
            try
            {
                actualizada = await _repo.ActualizarAsync(existente);

                // null = conservar la lista actual; lista vacía = quitar todas
                if (dto.AlmacenesConConteoIds != null)
                    await _repo.ReemplazarAlmacenesConteoAsync(actualizada.Id, dto.AlmacenesConConteoIds);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Temporada actualizada: {Id}", actualizada.Id);

            return await MapearAsync(actualizada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar temporada {Id}", dto.Id);
            throw;
        }
    }

    public async Task<TemporadaDto> CerrarAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Temporada", id);

            if (existente.Estado == EstadoTemporada.Cerrada)
                throw new ExcepcionDominio("La temporada ya está cerrada", 409, "TEMPORADA_CERRADA");

            // Cierre definitivo: no existe reapertura
            existente.Estado = EstadoTemporada.Cerrada;
            existente.FechaCierre = BoliviaTimeZone.Now().Date;

            var actualizada = await _repo.ActualizarAsync(existente);

            _logger.LogInformation("Temporada cerrada: {Id}", actualizada.Id);

            return await MapearAsync(actualizada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cerrar temporada {Id}", id);
            throw;
        }
    }

    private async Task<TemporadaDto> MapearAsync(Temporada entidad)
    {
        var dto = _mapper.Map<TemporadaDto>(entidad);
        await CompletarAlmacenesConteoAsync(new List<TemporadaDto> { dto });
        return dto;
    }

    private async Task CompletarAlmacenesConteoAsync(List<TemporadaDto> items)
    {
        if (items.Count == 0) return;

        var conteos = await _repo.ObtenerAlmacenesConteoAsync(items.Select(i => i.Id));
        if (conteos.Count == 0) return;

        var almacenIds = conteos.Select(c => c.AlmacenId).Distinct().ToList();
        var nombres = await _almacenRepo.AsQueryable()
            .Where(a => almacenIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Nombre })
            .ToDictionaryAsync(a => a.Id, a => a.Nombre);

        var porTemporada = conteos.ToLookup(c => c.TemporadaId);
        foreach (var item in items)
        {
            item.AlmacenesConConteo = porTemporada[item.Id]
                .Select(c => new TemporadaAlmacenConteoDto
                {
                    AlmacenId = c.AlmacenId,
                    // Si el almacén ya no existe en AuthDB, nombre vacío
                    Nombre = nombres.TryGetValue(c.AlmacenId, out var nombre) ? nombre : string.Empty
                })
                .OrderBy(x => x.Nombre)
                .ToList();
        }
    }

    // Cada almacén con conteo diario debe ser un almacén activo del sistema Navideño marcado como tienda
    private async Task ValidarAlmacenesConteoAsync(List<int>? almacenIds)
    {
        if (almacenIds == null || almacenIds.Count == 0) return;

        var ids = almacenIds.Distinct().ToList();
        var almacenes = await _almacenRepo.AsQueryable()
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id);

        foreach (var id in ids)
        {
            if (!almacenes.TryGetValue(id, out var almacen))
                throw new ValidacionException($"El almacén {id} no es una tienda navideña activa");

            if (!almacen.IsActive || almacen.SistemaId != SistemaNavidadId || !almacen.EsTienda)
                throw new ValidacionException($"El almacén {almacen.Nombre} no es una tienda navideña activa");
        }
    }
}
