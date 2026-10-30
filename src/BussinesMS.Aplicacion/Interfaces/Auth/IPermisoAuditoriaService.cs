using BussinesMS.Aplicacion.DTOs.Auth;

namespace BussinesMS.Aplicacion.Interfaces.Auth;

public interface IPermisoAuditoriaService
{
    Task<List<PermisoAuditoriaResumenDto>> ObtenerResumenAsync(PermisoAuditoriaFiltroDto filtro);
}
