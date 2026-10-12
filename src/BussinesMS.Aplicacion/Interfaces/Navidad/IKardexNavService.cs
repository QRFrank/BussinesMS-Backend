using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IKardexNavService
{
    Task<PagedResultDto<KardexNavDto>> ObtenerAsync(KardexNavFiltroDto query);
}
