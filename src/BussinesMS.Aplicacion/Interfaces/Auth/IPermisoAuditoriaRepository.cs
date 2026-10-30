using BussinesMS.Aplicacion.DTOs.Auth;
using BussinesMS.Dominio.Entidades.Auth;

namespace BussinesMS.Aplicacion.Interfaces.Auth;

public interface IPermisoAuditoriaRepository
{
    Task RegistrarAsync(PermisoAuditoria registro);
    Task<List<PermisoAuditoriaResumenDto>> ObtenerResumenAsync(PermisoAuditoriaFiltroDto filtro);
}
