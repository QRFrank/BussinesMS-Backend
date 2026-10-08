using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Auth;

namespace BussinesMS.Aplicacion.Interfaces.Auth;

public interface IUsuarioRepository : IRepositorio<Usuario>
{
    Task<Usuario?> ObtenerPorUsernameAsync(string username);
    Task<Usuario?> ValidarCredencialesAsync(string username, string password);
    Task<Usuario?> ObtenerConRolAsync(int id);
    Task<Usuario?> ObtenerConRolAsyncPorUsername(string username);
    Task<List<UsuarioMenu>> ObtenerMenusAsync(int usuarioId);
    Task AgregarMenusAsync(int usuarioId, List<UsuarioMenu> menus);
    Task EliminarMenusAsync(int usuarioId);
    Task<Usuario> ReactivarAsync(int id);
    Task<List<int>> ObtenerSistemaIdsAsync(int usuarioId);
    Task ReemplazarSistemasAsync(int usuarioId, List<int> sistemaIds);
}