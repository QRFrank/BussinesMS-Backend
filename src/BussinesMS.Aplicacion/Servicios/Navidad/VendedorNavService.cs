using AutoMapper;
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
// Los datos del usuario vienen de AuthDB (solo lectura) y se completan en memoria.
public class VendedorNavService : IVendedorNavService
{
    private readonly IVendedorNavRepository _repo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly IMapper _mapper;
    private readonly ILogger<VendedorNavService> _logger;

    public VendedorNavService(
        IVendedorNavRepository repo,
        IUsuarioRepository usuarioRepo,
        ITemporadaActualService temporadaActual,
        IMapper mapper,
        ILogger<VendedorNavService> logger)
    {
        _repo = repo;
        _usuarioRepo = usuarioRepo;
        _temporadaActual = temporadaActual;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<VendedorNavDto>> ObtenerTodosAsync(VendedorFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var entidades = await _repo.ObtenerActivosPorTemporadaAsync(temporadaId);
            var items = _mapper.Map<List<VendedorNavDto>>(entidades);
            await CompletarUsuariosAsync(items);

            // Filtros, orden y paginado en memoria (los nombres viven en AuthDB)
            IEnumerable<VendedorNavDto> filtrados = items;

            if (query.Tipo.HasValue)
                filtrados = filtrados.Where(x => (int)x.Tipo == query.Tipo.Value);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.Trim().ToLower();
                filtrados = filtrados.Where(x =>
                    x.UsuarioNombreCompleto.ToLower().Contains(f) ||
                    x.Username.ToLower().Contains(f));
            }

            var ordenados = string.IsNullOrWhiteSpace(query.SortBy)
                ? filtrados.AsQueryable().OrderBy(x => x.UsuarioNombreCompleto).ThenBy(x => x.Id)
                : filtrados.AsQueryable().ApplySorting(query);

            var lista = ordenados.ToList();
            var totalCount = lista.Count;

            if (query.GetIsPagedValue())
                lista = lista
                    .Skip((query.GetPageValue() - 1) * query.GetPageSizeValue())
                    .Take(query.GetPageSizeValue())
                    .ToList();

            return new PagedResultDto<VendedorNavDto>
            {
                Items = lista,
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener vendedores de temporada");
            throw;
        }
    }

    public async Task<VendedorNavDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerPorIdAsync(id);
            if (entidad == null || !entidad.IsActive) return null;
            return await MapearAsync(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener vendedor de temporada {Id}", id);
            throw;
        }
    }

    public async Task<List<UsuarioDisponibleNavDto>> ObtenerUsuariosDisponiblesAsync()
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            var ocupados = await _repo.ObtenerUsuarioIdsActivosAsync(temporada.Id);

            var usuarios = await _usuarioRepo.AsQueryable()
                .AsNoTracking()
                .ValidosNavidad()
                .Where(u => !ocupados.Contains(u.Id))
                .Select(u => new { u.Id, u.Nombre, u.Apellido, u.Username })
                .ToListAsync();

            return usuarios
                .Select(u => new UsuarioDisponibleNavDto
                {
                    UsuarioId = u.Id,
                    NombreCompleto = NombreCompleto(u.Nombre, u.Apellido),
                    Username = u.Username
                })
                .OrderBy(u => u.NombreCompleto)
                .ThenBy(u => u.Username)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios disponibles para vendedores");
            throw;
        }
    }

    public async Task<VendedorNavDto> CrearAsync(CrearVendedorNavDto dto)
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();

            var sueldoMensual = NormalizarReglas(dto.Tipo, dto.SueldoMensual);
            await ValidarUsuarioAsync(dto.UsuarioId);

            if (await _repo.ExisteUsuarioAsync(temporada.Id, dto.UsuarioId))
                throw new ExcepcionDominio("El usuario ya es vendedor en esta temporada", 409, "ENTIDAD_DUPLICADA");

            var entidad = new Vendedor
            {
                TemporadaId = temporada.Id,
                UsuarioId = dto.UsuarioId,
                Tipo = dto.Tipo,
                SueldoMensual = sueldoMensual
            };

            var creado = await _repo.CrearAsync(entidad);

            _logger.LogInformation("Vendedor de temporada creado: {Id} (temporada {TemporadaId})", creado.Id, creado.TemporadaId);

            return await MapearAsync(creado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear vendedor de temporada");
            throw;
        }
    }

    public async Task<VendedorNavDto> ActualizarAsync(ActualizarVendedorNavDto dto)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(dto.Id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Vendedor", dto.Id);

            await _temporadaActual.VerificarEditableAsync(existente.TemporadaId);

            var sueldoMensual = NormalizarReglas(dto.Tipo, dto.SueldoMensual);

            // Solo si cambia el usuario se revalida acceso y unicidad
            if (dto.UsuarioId != existente.UsuarioId)
            {
                await ValidarUsuarioAsync(dto.UsuarioId);

                if (await _repo.ExisteUsuarioAsync(existente.TemporadaId, dto.UsuarioId, existente.Id))
                    throw new ExcepcionDominio("El usuario ya es vendedor en esta temporada", 409, "ENTIDAD_DUPLICADA");
            }

            existente.UsuarioId = dto.UsuarioId;
            existente.Tipo = dto.Tipo;
            existente.SueldoMensual = sueldoMensual;

            var actualizado = await _repo.ActualizarAsync(existente);

            _logger.LogInformation("Vendedor de temporada actualizado: {Id}", actualizado.Id);

            return await MapearAsync(actualizado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar vendedor de temporada {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Vendedor", id);

            await _temporadaActual.VerificarEditableAsync(existente.TemporadaId);

            await _repo.EliminarAsync(id);
            _logger.LogInformation("Vendedor de temporada eliminado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar vendedor de temporada {Id}", id);
            throw;
        }
    }

    private async Task<VendedorNavDto> MapearAsync(Vendedor entidad)
    {
        var dto = _mapper.Map<VendedorNavDto>(entidad);
        await CompletarUsuariosAsync(new List<VendedorNavDto> { dto });
        return dto;
    }

    // Una sola consulta a AuthDB por Ids; si el usuario ya no existe, nombre vacío
    private async Task CompletarUsuariosAsync(List<VendedorNavDto> items)
    {
        if (items.Count == 0) return;

        var ids = items.Select(i => i.UsuarioId).Distinct().ToList();
        var usuarios = await _usuarioRepo.AsQueryable()
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Nombre, u.Apellido, u.Username })
            .ToDictionaryAsync(u => u.Id);

        foreach (var item in items)
        {
            if (usuarios.TryGetValue(item.UsuarioId, out var u))
            {
                item.UsuarioNombreCompleto = NombreCompleto(u.Nombre, u.Apellido);
                item.Username = u.Username;
            }
        }
    }

    private async Task ValidarUsuarioAsync(int usuarioId)
    {
        var valido = await _usuarioRepo.AsQueryable()
            .AsNoTracking()
            .ValidosNavidad()
            .AnyAsync(u => u.Id == usuarioId);

        if (!valido)
            throw new ValidacionException("El usuario no existe, está inactivo o no tiene acceso al sistema Navideño");
    }

    private static string NombreCompleto(string? nombre, string? apellido)
        => $"{nombre?.Trim()} {apellido?.Trim()}".Trim();

    // Reglas por tipo (siempre en servicio, no solo en FluentValidation). Devuelve el sueldo normalizado.
    private static decimal? NormalizarReglas(TipoVendedor tipo, decimal? sueldoMensual)
    {
        if (!Enum.IsDefined(typeof(TipoVendedor), tipo))
            throw new ValidacionException("El tipo de vendedor no es válido");

        if (tipo == TipoVendedor.Tienda)
        {
            if (!sueldoMensual.HasValue || sueldoMensual.Value <= 0)
                throw new ValidacionException("El sueldo mensual es obligatorio y debe ser mayor a 0 para vendedores de tienda");
            return sueldoMensual;
        }

        // Ruta: nunca lleva sueldo
        return null;
    }
}
