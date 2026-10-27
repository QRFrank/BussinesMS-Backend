using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// El rol se identifica por nombre (igual que "Admin"). Solo se consulta una vez por request.
public class AlmacenAccesoNavService : IAlmacenAccesoNavService
{
    public const string RolEncargado = "EncargadoAlmacen";
    public const string RolAdmin = "Admin";

    private readonly ITemporadaAlmacenEncargadoRepository _repo;
    private readonly IRolRepository _rolRepo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly ICurrentUserService _currentUser;

    private string? _rolNombre;
    private bool _rolCargado;
    private readonly Dictionary<int, List<int>> _cache = new();

    public AlmacenAccesoNavService(
        ITemporadaAlmacenEncargadoRepository repo,
        IRolRepository rolRepo,
        IAlmacenRepository almacenRepo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _rolRepo = rolRepo;
        _almacenRepo = almacenRepo;
        _currentUser = currentUser;
    }

    public async Task<bool> EsEncargadoAsync()
        => string.Equals(await ObtenerRolNombreAsync(), RolEncargado, StringComparison.OrdinalIgnoreCase);

    public async Task<bool> EsAdminAsync()
        => string.Equals(await ObtenerRolNombreAsync(), RolAdmin, StringComparison.OrdinalIgnoreCase);

    public async Task<List<int>> ObtenerAlmacenesACargoAsync(int temporadaId)
    {
        if (_cache.TryGetValue(temporadaId, out var cacheado)) return cacheado;

        var usuarioId = _currentUser.GetUsuarioId();
        var lista = usuarioId.HasValue
            ? await _repo.ObtenerAlmacenIdsDeUsuarioAsync(temporadaId, usuarioId.Value)
            : new List<int>();
        _cache[temporadaId] = lista;
        return lista;
    }

    public async Task<List<int>?> ObtenerRestriccionAsync(int temporadaId)
        => await EsEncargadoAsync() ? await ObtenerAlmacenesACargoAsync(temporadaId) : null;

    public async Task VerificarAlmacenAsync(int temporadaId, int almacenId)
        => await VerificarAlmacenesAsync(temporadaId, new[] { almacenId });

    public async Task VerificarAlmacenesAsync(int temporadaId, IEnumerable<int> almacenIds)
    {
        if (!await EsEncargadoAsync()) return;

        var aCargo = await ObtenerAlmacenesACargoAsync(temporadaId);
        var ajenos = almacenIds.Distinct().Where(id => !aCargo.Contains(id)).ToList();
        if (ajenos.Count == 0) return;

        var nombres = await _almacenRepo.AsQueryable()
            .AsNoTracking()
            .Where(a => ajenos.Contains(a.Id))
            .Select(a => a.Nombre)
            .ToListAsync();
        var texto = nombres.Count > 0 ? string.Join(", ", nombres) : string.Join(", ", ajenos.Select(i => $"#{i}"));
        throw new ExcepcionDominio($"El almacén {texto} no está asignado a tu usuario en esta temporada", 403, "ALMACEN_NO_ASIGNADO");
    }

    private async Task<string?> ObtenerRolNombreAsync()
    {
        if (_rolCargado) return _rolNombre;
        var rolId = _currentUser.GetRolId();
        if (rolId.HasValue)
        {
            var rol = await _rolRepo.ObtenerPorIdAsync(rolId.Value);
            _rolNombre = rol?.Nombre;
        }
        _rolCargado = true;
        return _rolNombre;
    }
}
