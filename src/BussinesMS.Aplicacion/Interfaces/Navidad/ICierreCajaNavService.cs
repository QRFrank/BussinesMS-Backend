using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ICierreCajaNavService
{
    Task<CierreCajaResumenNavDto> ObtenerResumenAsync(int almacenId);
    Task<CierreCajaResumenNavDto> ObtenerResumenPorIdAsync(int id);
    Task<CierreCajaNavDto> RegistrarAsync(RegistrarCierreCajaNavDto dto);
    Task<PagedResultDto<CierreCajaNavDto>> ObtenerHistorialAsync(CierreCajaNavFiltroDto query);
}
