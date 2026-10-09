using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ICompraNavService
{
    Task<PagedResultDto<CompraNavDto>> ObtenerTodosAsync(CompraNavFiltroDto query);
    Task<CompraNavDto?> ObtenerPorIdAsync(int id);
    Task<CompraNavDto> CrearAsync(CrearCompraNavDto dto);
    Task AnularAsync(int id);
}
