using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IPagoInversorService
{
    Task<PagedResultDto<PagoInversorDto>> ObtenerTodosAsync(PagoInversorFiltroDto query);
    Task<PagoInversorDto> CrearAsync(CrearPagoInversorDto dto);
    Task AnularAsync(int id);
}
