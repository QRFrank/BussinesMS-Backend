using BussinesMS.Aplicacion.DTOs.Auth;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Auth;

public interface IUsuarioService
{
    Task<PagedResultDto<UsuarioDto>> ObtenerTodosAsync(UsuarioFiltroDto query);
    Task<UsuarioConMenusDto?> ObtenerPorIdAsync(int id);
    Task<(UsuarioDto Entidad, bool FueReactivada)> CrearAsync(CrearUsuarioDto usuario);
    Task<UsuarioDto> ActualizarAsync(int id, ActualizarUsuarioDto dto);
    Task<List<MenuArbolDto>> ObtenerMenusAsync(int id);
    Task<UsuarioDto> ActualizarMenusAsync(int id, List<MenuPermisoSimpleDto> menus);
    Task<LoginResponseDto?> ValidarLoginAsync(string username, string password);
    Task CambiarPasswordAsync(int id, string password);
    Task EliminarAsync(int id);
    Task ReactivarAsync(int id);
}
