using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IProveedorNavService
{
    Task<PagedResultDto<ProveedorNavDto>> ObtenerTodosAsync(ProveedorNavFiltroDto query);
    Task<ProveedorNavDto?> ObtenerPorIdAsync(int id);
    Task<ProveedorNavDto> CrearAsync(CrearProveedorNavDto dto);
    Task<ProveedorNavDto> ActualizarAsync(ActualizarProveedorNavDto dto);
    Task EliminarAsync(int id);
}
