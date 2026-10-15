using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IConsumoNavService
{
    Task<PagedResultDto<ConsumoNavDto>> ObtenerTodosAsync(ConsumoNavFiltroDto query);
    // Con detalle y costo estimado (unidades × costo promedio)
    Task<ConsumoNavDto?> ObtenerPorIdAsync(int id);
    Task<ConsumoNavDto> CrearAsync(CrearConsumoNavDto dto);
    Task AnularAsync(int id);
}
