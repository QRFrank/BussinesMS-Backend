using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProveedorNav = BussinesMS.Dominio.Entidades.Navidad.Proveedor;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Nota: CreatedAt se convierte a hora de Bolivia en NavidadMappingProfile (no reconvertir aquí).
public class CodigoClienteService : ICodigoClienteService
{
    private readonly ICodigoClienteRepository _repo;
    private readonly IProveedorNavRepository _proveedorRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly IMapper _mapper;
    private readonly ILogger<CodigoClienteService> _logger;

    public CodigoClienteService(
        ICodigoClienteRepository repo,
        IProveedorNavRepository proveedorRepo,
        ITemporadaActualService temporadaActual,
        IMapper mapper,
        ILogger<CodigoClienteService> logger)
    {
        _repo = repo;
        _proveedorRepo = proveedorRepo;
        _temporadaActual = temporadaActual;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<CodigoClienteDto>> ObtenerTodosAsync(CodigoClienteFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var baseQuery = _repo.AsQueryable()
                .Include(x => x.Proveedor)
                .Where(x => x.IsActive && x.TemporadaId == temporadaId);

            if (query.ProveedorId.HasValue)
                baseQuery = baseQuery.Where(x => x.ProveedorId == query.ProveedorId.Value);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    x.Codigo.ToLower().Contains(f) ||
                    x.Titular.ToLower().Contains(f) ||
                    (x.Proveedor != null && x.Proveedor.Nombre.ToLower().Contains(f)));
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery
                    .OrderBy(x => x.Proveedor != null ? x.Proveedor.Nombre : string.Empty)
                    .ThenBy(x => x.Codigo)
                    .ThenBy(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            return new PagedResultDto<CodigoClienteDto>
            {
                Items = _mapper.Map<List<CodigoClienteDto>>(entidades),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener códigos de cliente");
            throw;
        }
    }

    public async Task<CodigoClienteDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerConDetallesAsync(id);
            if (entidad == null || !entidad.IsActive) return null;
            return _mapper.Map<CodigoClienteDto>(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener código de cliente {Id}", id);
            throw;
        }
    }

    public async Task<CodigoClienteDto> CrearAsync(CrearCodigoClienteDto dto)
    {
        try
        {
            var proveedor = await _proveedorRepo.ObtenerPorIdAsync(dto.ProveedorId);
            if (proveedor == null || !proveedor.IsActive)
                throw new ValidacionException("El proveedor no existe o no está activo");

            // El proveedor debe ser de la temporada abierta
            await _temporadaActual.VerificarEditableAsync(proveedor.TemporadaId);
            var abierta = await _temporadaActual.ObtenerAbiertaAsync();
            if (proveedor.TemporadaId != abierta.Id)
                throw new ValidacionException("El proveedor no pertenece a la temporada abierta");

            ValidarUsaCodigos(proveedor);

            var (codigo, titular) = ValidarReglas(dto.Codigo, dto.Titular);
            if (await _repo.ExisteCodigoAsync(proveedor.Id, codigo))
                throw new ExcepcionDominio($"Ya existe el código '{codigo}' para este proveedor", 409, "ENTIDAD_DUPLICADA");

            var entidad = new CodigoCliente
            {
                TemporadaId = proveedor.TemporadaId,
                ProveedorId = proveedor.Id,
                Codigo = codigo,
                Titular = titular
            };

            var creada = await _repo.CrearAsync(entidad);

            _logger.LogInformation("Código de cliente creado: {Id} (proveedor {ProveedorId})", creada.Id, creada.ProveedorId);

            var conDetalles = await _repo.ObtenerConDetallesAsync(creada.Id) ?? creada;
            return _mapper.Map<CodigoClienteDto>(conDetalles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear código de cliente");
            throw;
        }
    }

    public async Task<CodigoClienteDto> ActualizarAsync(ActualizarCodigoClienteDto dto)
    {
        try
        {
            var existente = await _repo.ObtenerConDetallesAsync(dto.Id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Código de cliente", dto.Id);

            await _temporadaActual.VerificarEditableAsync(existente.TemporadaId);

            var proveedor = existente.Proveedor;
            if (proveedor == null || !proveedor.IsActive)
                throw new ValidacionException("El proveedor no existe o no está activo");
            ValidarUsaCodigos(proveedor);

            var (codigo, titular) = ValidarReglas(dto.Codigo, dto.Titular);
            if (await _repo.ExisteCodigoAsync(existente.ProveedorId, codigo, existente.Id))
                throw new ExcepcionDominio($"Ya existe el código '{codigo}' para este proveedor", 409, "ENTIDAD_DUPLICADA");

            existente.Codigo = codigo;
            existente.Titular = titular;

            var actualizada = await _repo.ActualizarAsync(existente);

            _logger.LogInformation("Código de cliente actualizado: {Id}", actualizada.Id);

            return _mapper.Map<CodigoClienteDto>(actualizada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar código de cliente {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Código de cliente", id);

            await _temporadaActual.VerificarEditableAsync(existente.TemporadaId);

            await _repo.EliminarAsync(id);
            _logger.LogInformation("Código de cliente eliminado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar código de cliente {Id}", id);
            throw;
        }
    }

    private static void ValidarUsaCodigos(ProveedorNav proveedor)
    {
        if (!proveedor.UsaCodigosCliente)
            throw new ValidacionException("El proveedor no usa códigos de cliente");
    }

    // Reglas repetidas del validador por si FluentValidation no corre
    private static (string Codigo, string Titular) ValidarReglas(string? codigo, string? titular)
    {
        var c = codigo?.Trim() ?? string.Empty;
        var t = titular?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(c))
            throw new ValidacionException("El código es obligatorio");
        if (c.Length > 50)
            throw new ValidacionException("El código no puede superar 50 caracteres");
        if (string.IsNullOrWhiteSpace(t))
            throw new ValidacionException("El titular es obligatorio");
        if (t.Length > 150)
            throw new ValidacionException("El titular no puede superar 150 caracteres");
        return (c, t);
    }
}
