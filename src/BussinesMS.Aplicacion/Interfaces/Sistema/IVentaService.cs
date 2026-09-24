using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface IVentaService
{
    Task<PagedResultDto<VentaListDto>> ObtenerTodosAsync(VentaFiltroDto query);
    Task<VentaDto?> ObtenerPorIdAsync(int id);
    Task<VentaDto> CrearAsync(CrearVentaDto dto);
    Task EliminarAsync(int id);
}
