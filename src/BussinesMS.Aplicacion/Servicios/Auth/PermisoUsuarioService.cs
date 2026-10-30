using System.Collections.Concurrent;
using BussinesMS.Aplicacion.DTOs.Auth;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Aplicacion.Servicios.Navidad;

namespace BussinesMS.Aplicacion.Servicios.Auth;

// Soporte del filtro de autorización global (permisos fase 2): como máximo 1 consulta de rol
// (compartida con IAlmacenAccesoNavService) + 1 de menús por request. Sin try/catch: se llama en cada request.
public class PermisoUsuarioService : IPermisoUsuarioService
{
    private static readonly ConcurrentDictionary<int, string> _nombresMenus = new();

    private readonly IAlmacenAccesoNavService _acceso;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IMenuRepository _menuRepo;
    private readonly ICurrentUserService _currentUser;

    private IReadOnlyDictionary<int, FlagsMenuDto>? _flags;
    private bool? _puedeHacerRecepciones;

    public PermisoUsuarioService(
        IAlmacenAccesoNavService acceso,
        IUsuarioRepository usuarioRepo,
        IMenuRepository menuRepo,
        ICurrentUserService currentUser)
    {
        _acceso = acceso;
        _usuarioRepo = usuarioRepo;
        _menuRepo = menuRepo;
        _currentUser = currentUser;
    }

    public Task<bool> EsAdminAsync() => _acceso.EsAdminAsync();

    public async Task<IReadOnlyDictionary<int, FlagsMenuDto>> ObtenerFlagsAsync()
    {
        if (_flags != null) return _flags;

        var usuarioId = _currentUser.GetUsuarioId();
        if (!usuarioId.HasValue || usuarioId.Value <= 0)
        {
            _flags = new Dictionary<int, FlagsMenuDto>();
            return _flags;
        }

        var menus = await _usuarioRepo.ObtenerMenusAsync(usuarioId.Value);
        var flags = new Dictionary<int, FlagsMenuDto>();
        foreach (var um in menus)
        {
            flags[um.MenuId] = new FlagsMenuDto
            {
                Leer = um.Leer,
                Crear = um.Crear,
                Editar = um.Editar,
                Eliminar = um.Eliminar
            };
        }
        _flags = flags;
        return _flags;
    }

    public async Task<bool> PuedeHacerRecepcionesAsync()
    {
        if (_puedeHacerRecepciones.HasValue) return _puedeHacerRecepciones.Value;

        var usuarioId = _currentUser.GetUsuarioId();
        _puedeHacerRecepciones = usuarioId.HasValue && usuarioId.Value > 0 &&
            await _usuarioRepo.ObtenerPuedeHacerRecepcionesAsync(usuarioId.Value, UsuarioNavidadFiltros.SistemaNavidadId);
        return _puedeHacerRecepciones.Value;
    }

    public async Task<IReadOnlyDictionary<int, string>> ObtenerNombresMenusAsync(IEnumerable<int> menuIds)
    {
        var ids = menuIds.Distinct().ToList();

        if (ids.Any(id => !_nombresMenus.ContainsKey(id)))
        {
            var menus = await _menuRepo.ObtenerTodosAsync();
            foreach (var menu in menus)
                _nombresMenus[menu.Id] = menu.Nombre;
        }

        var resultado = new Dictionary<int, string>();
        foreach (var id in ids)
            resultado[id] = _nombresMenus.TryGetValue(id, out var nombre) ? nombre : $"menú {id}";
        return resultado;
    }
}
