using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ITrasladoNavService
{
    Task<PagedResultDto<TrasladoNavDto>> ObtenerTodosAsync(TrasladoNavFiltroDto query);
    Task<TrasladoNavDto?> ObtenerPorIdAsync(int id);
    Task<TrasladoNavDto> CrearAsync(CrearTrasladoNavDto dto);
    Task AnularAsync(int id);
}
