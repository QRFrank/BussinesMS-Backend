using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Nota: CreatedAt se convierte a hora de Bolivia en NavidadMappingProfile (no reconvertir aquí).
public class GastoNavService : IGastoNavService
{
    private readonly IGastoNavRepository _repo;
    private readonly ICategoriaGastoNavRepository _categoriaRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly IMapper _mapper;
    private readonly ILogger<GastoNavService> _logger;

    public GastoNavService(
        IGastoNavRepository repo,
        ICategoriaGastoNavRepository categoriaRepo,
        ITemporadaActualService temporadaActual,
        IMapper mapper,
        ILogger<GastoNavService> logger)
    {
        _repo = repo;
        _categoriaRepo = categoriaRepo;
        _temporadaActual = temporadaActual;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<GastoNavPagedResultDto> ObtenerTodosAsync(GastoNavFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var baseQuery = _repo.AsQueryable()
                .Include(x => x.Categoria)
                .Where(x => x.IsActive && x.TemporadaId == temporadaId);

            if (query.CategoriaId.HasValue)
                baseQuery = baseQuery.Where(x => x.CategoriaId == query.CategoriaId.Value);

            // Fecha es columna date (fecha local), se compara sin conversión de zona horaria
            if (query.FechaDesde.HasValue)
            {
                var desde = query.FechaDesde.Value.Date;
                baseQuery = baseQuery.Where(x => x.Fecha >= desde);
            }

            if (query.FechaHasta.HasValue)
            {
                var hasta = query.FechaHasta.Value.Date;
                baseQuery = baseQuery.Where(x => x.Fecha <= hasta);
            }

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    x.Descripcion.ToLower().Contains(f) ||
                    (x.Categoria != null && x.Categoria.Nombre.ToLower().Contains(f)));
            }

            // Total del resultado filtrado completo, antes de paginar
            var totalMonto = await baseQuery.SumAsync(x => (decimal?)x.Monto) ?? 0m;

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            return new GastoNavPagedResultDto
            {
                Items = _mapper.Map<List<GastoNavDto>>(entidades),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue(),
                TotalMonto = totalMonto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener gastos de temporada");
            throw;
        }
    }

    public async Task<GastoNavDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerConDetallesAsync(id);
            if (entidad == null || !entidad.IsActive) return null;
            return _mapper.Map<GastoNavDto>(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener gasto de temporada {Id}", id);
            throw;
        }
    }

    public async Task<GastoNavDto> CrearAsync(CrearGastoNavDto dto)
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();

            ValidarReglas(dto.Descripcion, dto.Monto);
            await ValidarCategoriaAsync(dto.CategoriaId);

            var entidad = new GastoNav
            {
                TemporadaId = temporada.Id,
                CategoriaId = dto.CategoriaId,
                Fecha = dto.Fecha.Date,
                Descripcion = dto.Descripcion.Trim(),
                Monto = dto.Monto
            };

            var creada = await _repo.CrearAsync(entidad);

            _logger.LogInformation("Gasto de temporada creado: {Id} (temporada {TemporadaId})", creada.Id, creada.TemporadaId);

            var conDetalles = await _repo.ObtenerConDetallesAsync(creada.Id) ?? creada;
            return _mapper.Map<GastoNavDto>(conDetalles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear gasto de temporada");
            throw;
        }
    }

    public async Task<GastoNavDto> ActualizarAsync(ActualizarGastoNavDto dto)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(dto.Id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Gasto", dto.Id);

            await _temporadaActual.VerificarEditableAsync(existente.TemporadaId);

            ValidarReglas(dto.Descripcion, dto.Monto);
            await ValidarCategoriaAsync(dto.CategoriaId);

            existente.CategoriaId = dto.CategoriaId;
            existente.Fecha = dto.Fecha.Date;
            existente.Descripcion = dto.Descripcion.Trim();
            existente.Monto = dto.Monto;

            var actualizada = await _repo.ActualizarAsync(existente);

            _logger.LogInformation("Gasto de temporada actualizado: {Id}", actualizada.Id);

            var conDetalles = await _repo.ObtenerConDetallesAsync(actualizada.Id) ?? actualizada;
            return _mapper.Map<GastoNavDto>(conDetalles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar gasto de temporada {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Gasto", id);

            await _temporadaActual.VerificarEditableAsync(existente.TemporadaId);

            await _repo.EliminarAsync(id);
            _logger.LogInformation("Gasto de temporada eliminado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar gasto de temporada {Id}", id);
            throw;
        }
    }

    // Reglas repetidas del validador por si FluentValidation no corre
    private static void ValidarReglas(string? descripcion, decimal monto)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ValidacionException("La descripción es obligatoria");
        if (descripcion.Trim().Length > 500)
            throw new ValidacionException("La descripción no puede superar 500 caracteres");
        if (monto <= 0)
            throw new ValidacionException("El monto debe ser mayor a 0");
    }

    private async Task ValidarCategoriaAsync(int categoriaId)
    {
        var categoria = await _categoriaRepo.ObtenerPorIdAsync(categoriaId);
        if (categoria == null || !categoria.IsActive)
            throw new ValidacionException("La categoría no existe o no está activa");
    }
}
