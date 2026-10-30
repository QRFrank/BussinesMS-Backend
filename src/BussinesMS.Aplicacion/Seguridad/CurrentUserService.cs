namespace BussinesMS.Aplicacion.Seguridad;

public interface ICurrentUserService
{
    int? GetUsuarioId();
    string? GetUsername();
    int? GetRolId();
    void SetUser(int usuarioId, string username, int rolId);
    void Clear();
}

public class CurrentUserService : ICurrentUserService
{
    private int? _usuarioId;
    private string? _username;
    private int? _rolId;

    public int? GetUsuarioId() => _usuarioId;
    public string? GetUsername() => _username;
    public int? GetRolId() => _rolId;

    public void SetUser(int usuarioId, string username, int rolId)
    {
        _usuarioId = usuarioId;
        _username = username;
        _rolId = rolId;
    }

    public void Clear()
    {
        _usuarioId = null;
        _username = null;
        _rolId = null;
    }
}

/// <summary>
/// Usuario "de sistema" para procesos sin request HTTP (VencimientoLotesJob, seeders).
/// Hoy es el admin con Id = 1 (creado por el seed). Los requests HTTP NUNCA lo usan como respaldo:
/// si falta el token se responde 401 (ver <see cref="CurrentUserServiceExtensions.ObtenerUsuarioIdRequerido"/>).
/// </summary>
public static class UsuarioDeSistema
{
    public const int Id = 1;
    public const string Username = "sistema";
}

public static class CurrentUserServiceExtensions
{
    /// <summary>Id del usuario autenticado; si no hay sesion lanza 401 (NO_AUTENTICADO).</summary>
    public static int ObtenerUsuarioIdRequerido(this ICurrentUserService currentUser)
    {
        var id = currentUser.GetUsuarioId();
        if (id is int valor && valor > 0) return valor;
        throw new BussinesMS.Dominio.Excepciones.ExcepcionDominio(
            "La sesión venció o no existe. Iniciá sesión nuevamente.", 401, "NO_AUTENTICADO");
    }
}
