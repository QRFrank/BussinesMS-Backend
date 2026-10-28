using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

public class PrecioCompraAccesoService : IPrecioCompraAccesoService
{
    private readonly IAlmacenAccesoNavService _acceso;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly ICurrentUserService _currentUser;

    private bool? _cache;

    public PrecioCompraAccesoService(
        IAlmacenAccesoNavService acceso,
        IUsuarioRepository usuarioRepo,
        ICurrentUserService currentUser)
    {
        _acceso = acceso;
        _usuarioRepo = usuarioRepo;
        _currentUser = currentUser;
    }

    public async Task<bool> PuedeVerPreciosCompraAsync()
    {
        if (_cache.HasValue) return _cache.Value;

        if (await _acceso.EsAdminAsync())
        {
            _cache = true;
            return true;
        }

        var usuarioId = _currentUser.GetUsuarioId();
        _cache = usuarioId.HasValue &&
                 await _usuarioRepo.ObtenerVerPreciosCompraAsync(usuarioId.Value, UsuarioNavidadFiltros.SistemaNavidadId);
        return _cache.Value;
    }
}
