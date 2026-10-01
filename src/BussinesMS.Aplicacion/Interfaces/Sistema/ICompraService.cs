using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface ICompraService
{
    Task<PagedResultDto<CompraListDto>> ObtenerTodosAsync(CompraFiltroDto query);
    Task<CompraDto?> ObtenerPorIdAsync(int id);
    Task<CompraDto> CrearAsync(CrearCompraDto dto);
    Task<CompraDto> ActualizarAsync(ActualizarCompraDto dto);
    Task EliminarAsync(int id);
}
