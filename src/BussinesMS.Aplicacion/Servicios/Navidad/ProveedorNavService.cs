using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProveedorNav = BussinesMS.Dominio.Entidades.Navidad.Proveedor;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Nota: CreatedAt se convierte a hora de Bolivia en NavidadMappingProfile (no reconvertir aquí).
public class ProveedorNavService : IProveedorNavService
{
    private readonly IProveedorNavRepository _repo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly IMapper _mapper;
    private readonly ILogger<ProveedorNavService> _logger;

    public ProveedorNavService(
        IProveedorNavRepository repo,
        ITemporadaActualService temporadaActual,
        IMapper mapper,
        ILogger<ProveedorNavService> logger)
    {
        _repo = repo;
        _temporadaActual = temporadaActual;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<ProveedorNavDto>> ObtenerTodosAsync(ProveedorNavFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var baseQuery = _repo.AsQueryable()
                .Where(x => x.IsActive && x.TemporadaId == temporadaId);

            if (query.UsaCodigosCliente.HasValue)
                baseQuery = baseQuery.Where(x => x.UsaCodigosCliente == query.UsaCodigosCliente.Value);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    x.Nombre.ToLower().Contains(f) ||
                    (x.Telefono != null && x.Telefono.ToLower().Contains(f)));
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderBy(x => x.Nombre).ThenBy(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            // Conteos con una consulta agrupada por tipo (sin N+1)
            var ids = entidades.Select(x => x.Id).ToList();
            var codigos = await _repo.ContarCodigosActivosAsync(ids);
            var productos = await _repo.ContarProductosActivosAsync(ids);

            var items = entidades.Select(e =>
            {
                var dto = _mapper.Map<ProveedorNavDto>(e);
                dto.CantidadCodigos = codigos.GetValueOrDefault(e.Id);
                dto.CantidadProductos = productos.GetValueOrDefault(e.Id);
                return dto;
            }).ToList();

            return new PagedResultDto<ProveedorNavDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener proveedores de temporada");
            throw;
        }
    }

    public async Task<ProveedorNavDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerConCodigosAsync(id);
            if (entidad == null || !entidad.IsActive) return null;
            return await MapearDetalleAsync(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener proveedor de temporada {Id}", id);
            throw;
        }
    }

    public async Task<ProveedorNavDto> CrearAsync(CrearProveedorNavDto dto)
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();

            var nombre = ValidarNombre(dto.Nombre);
            if (await _repo.ExisteNombreAsync(temporada.Id, nombre))
                throw new EntidadDuplicadaException("un proveedor en esta temporada", nombre);

            var entidad = new ProveedorNav
            {
                TemporadaId = temporada.Id,
                Nombre = nombre,
                UsaCodigosCliente = dto.UsaCodigosCliente,
                Telefono = Limpiar(dto.Telefono),
                Observacion = Limpiar(dto.Observacion)
            };

            var creada = await _repo.CrearAsync(entidad);

            _logger.LogInformation("Proveedor de temporada creado: {Id} (temporada {TemporadaId})", creada.Id, creada.TemporadaId);

            // Recién creado: sin códigos ni productos
            return _mapper.Map<ProveedorNavDto>(creada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear proveedor de temporada");
            throw;
        }
    }

    public async Task<ProveedorNavDto> ActualizarAsync(ActualizarProveedorNavDto dto)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(dto.Id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Proveedor", dto.Id);

            await _temporadaActual.VerificarEditableAsync(existente.TemporadaId);

            var nombre = ValidarNombre(dto.Nombre);
            if (await _repo.ExisteNombreAsync(existente.TemporadaId, nombre, existente.Id))
                throw new EntidadDuplicadaException("un proveedor en esta temporada", nombre);

            if (!dto.UsaCodigosCliente && await _repo.TieneCodigosActivosAsync(existente.Id))
                throw new ValidacionException("El proveedor tiene códigos de cliente activos; elimínelos antes de desactivar 'Usa códigos de cliente'");

            existente.Nombre = nombre;
            existente.UsaCodigosCliente = dto.UsaCodigosCliente;
            existente.Telefono = Limpiar(dto.Telefono);
            existente.Observacion = Limpiar(dto.Observacion);

            var actualizada = await _repo.ActualizarAsync(existente);

            _logger.LogInformation("Proveedor de temporada actualizado: {Id}", actualizada.Id);

            var conCodigos = await _repo.ObtenerConCodigosAsync(actualizada.Id) ?? actualizada;
            return await MapearDetalleAsync(conCodigos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar proveedor de temporada {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Proveedor", id);

            await _temporadaActual.VerificarEditableAsync(existente.TemporadaId);

            if (await _repo.TieneProductosActivosAsync(id) || await _repo.TieneCodigosActivosAsync(id))
                throw new ValidacionException("El proveedor tiene productos o códigos activos");

            await _repo.EliminarAsync(id);
            _logger.LogInformation("Proveedor de temporada eliminado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar proveedor de temporada {Id}", id);
            throw;
        }
    }

    // DTO con códigos activos ordenados por Codigo y conteos
    private async Task<ProveedorNavDto> MapearDetalleAsync(ProveedorNav entidad)
    {
        var dto = _mapper.Map<ProveedorNavDto>(entidad);
        var codigos = entidad.Codigos.Where(c => c.IsActive).OrderBy(c => c.Codigo).ToList();
        dto.Codigos = _mapper.Map<List<CodigoClienteDto>>(codigos);
        foreach (var c in dto.Codigos)
            c.ProveedorNombre = entidad.Nombre;
        dto.CantidadCodigos = codigos.Count;
        var productos = await _repo.ContarProductosActivosAsync(new[] { entidad.Id });
        dto.CantidadProductos = productos.GetValueOrDefault(entidad.Id);
        return dto;
    }

    // Reglas repetidas del validador por si FluentValidation no corre
    private static string ValidarNombre(string? nombre)
    {
        var n = nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(n))
            throw new ValidacionException("El nombre es obligatorio");
        if (n.Length > 150)
            throw new ValidacionException("El nombre no puede superar 150 caracteres");
        return n;
    }

    private static string? Limpiar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
