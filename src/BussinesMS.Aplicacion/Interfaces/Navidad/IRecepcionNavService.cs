using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IRecepcionNavService
{
    Task<PagedResultDto<RecepcionNavDto>> ObtenerTodosAsync(RecepcionNavFiltroDto query);
    Task<RecepcionNavDto?> ObtenerPorIdAsync(int id);
    Task<RecepcionNavDto> CrearAsync(CrearRecepcionNavDto dto);
    Task AnularAsync(int id);
}
