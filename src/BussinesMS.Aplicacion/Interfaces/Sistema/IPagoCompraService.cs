using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface IPagoCompraService
{
    Task<PagedResultDto<PagoCompraListDto>> ObtenerTodosAsync(PagoCompraFiltroDto query);
    Task<PagoCompraListDto?> ObtenerPorIdAsync(int id);
    Task<PagoCompraListDto> CrearAsync(CrearPagoCompraDto dto);
    Task<PagoCompraListDto> ActualizarAsync(ActualizarPagoCompraDto dto);
    Task AnularAsync(int id);
}
