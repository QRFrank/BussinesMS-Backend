using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface IInventarioLoteService
{
    Task<PagedResultDto<InventarioLoteDto>> ObtenerTodosAsync(GenericPaginationQueryDto query, int? categoriaId = null, int? almacenId = null, int? estadoLote = null);
    Task<InventarioLoteDto?> ObtenerPorIdAsync(int id);
    Task<InventarioLoteDto> CrearAsync(CrearInventarioLoteAlmacenDto dto);
    Task<InventarioLoteDto> ActualizarAsync(ActualizarInventarioLoteAlmacenDto dto);
    Task EliminarAsync(int id);
    Task<InventarioLoteDto> AjustarStockAsync(int id, AjusteInventarioDto dto);
}
