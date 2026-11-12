using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Almacenes navideños (AuthDB, solo lectura) + asignación de encargados por temporada (NavidadDb).
public class AlmacenNavService : IAlmacenNavService
{
    private const int SistemaNavidadId = 2;
    private const string TipoEncargadoRol = "encargado";
    private const string TipoEncargadoVendedorTienda = "vendedorTienda";

    private readonly ITemporadaAlmacenEncargadoRepository _repo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IVendedorNavRepository _vendedorRepo;
    private readonly ITemporadaRepository _temporadaRepo;
    private readonly IAlmacenAccesoNavService _acceso;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly ILogger<AlmacenNavService> _logger;

    public AlmacenNavService(
        ITemporadaAlmacenEncargadoRepository repo,
        IAlmacenRepository almacenRepo,
        IUsuarioRepository usuarioRepo,
        IVendedorNavRepository vendedorRepo,
        ITemporadaRepository temporadaRepo,
        IAlmacenAccesoNavService acceso,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        ILogger<AlmacenNavService> logger)
    {
        _repo = repo;
        _almacenRepo = almacenRepo;
        _usuarioRepo = usuarioRepo;
        _vendedorRepo = vendedorRepo;
        _temporadaRepo = temporadaRepo;
        _acceso = acceso;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _logger = logger;
    }

    public async Task<List<AlmacenNavDto>> ObtenerTodosAsync(AlmacenNavFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var consulta = _almacenRepo.AsQueryable().AsNoTracking().Where(a => a.SistemaId == SistemaNavidadId);
            if (query.SoloActivos == true)
                consulta = consulta.Where(a => a.IsActive);
            var almacenes = await consulta.OrderBy(a => a.Nombre).ToListAsync();

            var asignaciones = await _repo.ObtenerPorTemporadaAsync(temporadaId);
            var nombres = await ObtenerNombresUsuarioAsync(asignaciones.Select(x => x.UsuarioId));

            var restriccion = await _acceso.ObtenerRestriccionAsync(temporadaId);

            return almacenes.Select(a => new AlmacenNavDto
            {
                Id = a.Id,
                Nombre = a.Nombre,
                Codigo = a.Codigo,
                EsTienda = a.EsTienda,
                Direccion = a.Direccion,
                SistemaId = a.SistemaId,
                IsActive = a.IsActive,
                ACargo = restriccion == null || restriccion.Contains(a.Id),
                Encargados = asignaciones
                    .Where(x => x.AlmacenId == a.Id)
                    .Select(x => nombres.TryGetValue(x.UsuarioId, out var u) ? u : new EncargadoNavDto { UsuarioId = x.UsuarioId })
                    .OrderBy(e => e.Nombre)
                    .ToList()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener almacenes navideños");
            throw;
        }
    }

    public async Task<List<EncargadoNavDto>> ObtenerEncargadosAsync(int almacenId)
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            await ValidarAlmacenAsync(almacenId);

            var asignaciones = (await _repo.ObtenerPorTemporadaAsync(temporada.Id)).Where(x => x.AlmacenId == almacenId).ToList();
            var nombres = await ObtenerNombresUsuarioAsync(asignaciones.Select(x => x.UsuarioId));
            return asignaciones
                .Select(x => nombres.TryGetValue(x.UsuarioId, out var u) ? u : new EncargadoNavDto { UsuarioId = x.UsuarioId })
                .OrderBy(e => e.Nombre)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener encargados del almacén {AlmacenId}", almacenId);
            throw;
        }
    }

    public async Task<List<EncargadoNavDto>> ReemplazarEncargadosAsync(int almacenId, AsignarEncargadosNavDto dto)
    {
        try
        {
            if (!await _acceso.EsAdminAsync())
                throw new ExcepcionDominio("Solo el admin puede asignar encargados de almacén", 403, "SIN_PERMISO");

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            _temporadaActual.VerificarEditable(temporada);
            await ValidarAlmacenAsync(almacenId);

            var ids = (dto.UsuarioIds ?? new List<int>()).Distinct().ToList();
            if (ids.Count > 0)
            {
                var candidatos = (await ObtenerCandidatosAsync()).ToDictionary(c => c.UsuarioId);
                var invalidos = ids.Where(i => !candidatos.ContainsKey(i)).ToList();
                if (invalidos.Count > 0)
                    throw new ValidacionException(
                        $"Los usuarios {string.Join(", ", invalidos)} no son encargados de almacén ni vendedores de tienda activos con acceso al sistema Navideño");

                // Un vendedor de tienda solo puede quedar como encargado de una tienda (para recibir sus avisos)
                var vendedoresTienda = ids.Where(i => candidatos[i].Tipo == TipoEncargadoVendedorTienda).ToList();
                if (vendedoresTienda.Count > 0)
                {
                    var almacen = await _almacenRepo.AsQueryable().AsNoTracking().FirstAsync(a => a.Id == almacenId);
                    if (!almacen.EsTienda)
                    {
                        var nombre = candidatos[vendedoresTienda[0]].Nombre;
                        throw new ValidacionException(
                            $"{nombre} es vendedor de tienda: solo puede quedar como encargado de una tienda");
                    }
                }
            }

            await _uow.BeginTransactionAsync();
            try
            {
                await _repo.ReemplazarAsync(temporada.Id, almacenId, ids);
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Encargados del almacén {AlmacenId} actualizados (temporada {TemporadaId}): {Cantidad}", almacenId, temporada.Id, ids.Count);
            return await ObtenerEncargadosAsync(almacenId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asignar encargados del almacén {AlmacenId}", almacenId);
            throw;
        }
    }

    public async Task<List<EncargadoNavDto>> ObtenerCandidatosAsync()
    {
        // Vendedores de tienda activos de la temporada abierta (los de ruta tambien tienen el rol VendedorTienda: se distinguen por la tabla Vendedor)
        // Sin temporada abierta solo hay candidatos con rol EncargadoAlmacen (como antes)
        var temporada = await _temporadaRepo.ObtenerAbiertaAsync();
        var vendedoresTiendaIds = temporada == null
            ? new List<int>()
            : await _vendedorRepo.AsQueryable()
                .AsNoTracking()
                .Where(v => v.IsActive && v.TemporadaId == temporada.Id && v.Tipo == TipoVendedor.Tienda)
                .Select(v => v.UsuarioId)
                .Distinct()
                .ToListAsync();

        var usuarios = await _usuarioRepo.AsQueryable()
            .AsNoTracking()
            .ValidosNavidad()
            .Where(u => (u.Rol != null && u.Rol.Nombre == AlmacenAccesoNavService.RolEncargado) || vendedoresTiendaIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.Nombre,
                u.Apellido,
                u.Username,
                EsEncargado = u.Rol != null && u.Rol.Nombre == AlmacenAccesoNavService.RolEncargado
            })
            .ToListAsync();

        return usuarios
            .Select(u => new EncargadoNavDto
            {
                UsuarioId = u.Id,
                Nombre = $"{u.Nombre?.Trim()} {u.Apellido?.Trim()}".Trim(),
                Username = u.Username,
                Tipo = u.EsEncargado ? TipoEncargadoRol : TipoEncargadoVendedorTienda
            })
            .OrderBy(e => e.Nombre)
            .ToList();
    }

    // AuthDB: existe, es del sistema navideño y está activo
    private async Task ValidarAlmacenAsync(int almacenId)
    {
        var almacen = await _almacenRepo.AsQueryable().AsNoTracking().FirstOrDefaultAsync(a => a.Id == almacenId);
        if (almacen == null || !almacen.IsActive || almacen.SistemaId != SistemaNavidadId)
            throw new ValidacionException($"El almacén {almacenId} no es un almacén navideño activo");
    }

    private async Task<Dictionary<int, EncargadoNavDto>> ObtenerNombresUsuarioAsync(IEnumerable<int> usuarioIds)
    {
        var ids = usuarioIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, EncargadoNavDto>();
        var usuarios = await _usuarioRepo.AsQueryable()
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.Nombre,
                u.Apellido,
                u.Username,
                EsEncargado = u.Rol != null && u.Rol.Nombre == AlmacenAccesoNavService.RolEncargado
            })
            .ToListAsync();
        return usuarios.ToDictionary(u => u.Id, u => new EncargadoNavDto
        {
            UsuarioId = u.Id,
            Nombre = $"{u.Nombre?.Trim()} {u.Apellido?.Trim()}".Trim(),
            Username = u.Username,
            Tipo = u.EsEncargado ? TipoEncargadoRol : TipoEncargadoVendedorTienda
        });
    }
}
