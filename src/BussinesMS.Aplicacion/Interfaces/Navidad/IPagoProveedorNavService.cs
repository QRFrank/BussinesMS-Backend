using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IPagoProveedorNavService
{
    Task<PagedResultDto<PagoProveedorNavDto>> ObtenerTodosAsync(PagoProveedorNavFiltroDto query);
    Task<PagoProveedorNavDto?> ObtenerPorIdAsync(int id);
    Task<PagoProveedorNavDto> CrearAsync(CrearPagoProveedorNavDto dto);
    Task AnularAsync(int id);
}
