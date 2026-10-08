using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IAporteCapitalService
{
    Task<PagedResultDto<AporteCapitalDto>> ObtenerTodosAsync(AporteCapitalFiltroDto query);
    Task<AporteCapitalDto?> ObtenerPorIdAsync(int id);
    Task<ResumenCapitalDto> ObtenerResumenAsync(int? temporadaId);
    Task<AporteCapitalDto> CrearAsync(CrearAporteCapitalDto dto);
    Task<AporteCapitalDto> ActualizarAsync(ActualizarAporteCapitalDto dto);
    Task EliminarAsync(int id);
}
