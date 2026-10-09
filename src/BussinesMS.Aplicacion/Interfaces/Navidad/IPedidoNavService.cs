using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IPedidoNavService
{
    Task<PagedResultDto<PedidoNavDto>> ObtenerTodosAsync(PedidoNavFiltroDto query);
    Task<PedidoNavDto?> ObtenerPorIdAsync(int id);
    Task<PedidoNavDto> CrearAsync(CrearPedidoNavDto dto);
    Task<PedidoNavDto> ActualizarAsync(ActualizarPedidoNavDto dto);
}
