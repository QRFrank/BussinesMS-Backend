using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IProductoNavService
{
    Task<PagedResultDto<ProductoNavDto>> ObtenerTodosAsync(ProductoNavFiltroDto query);
    Task<ProductoNavDto?> ObtenerPorIdAsync(int id);
    Task<ProductoNavDto> CrearAsync(CrearProductoNavDto dto);
    Task<ProductoNavDto> ActualizarAsync(ActualizarProductoNavDto dto);
    Task EliminarAsync(int id);
    Task<ActualizarPreciosResultadoNavDto> ActualizarPreciosAsync(List<ActualizarPrecioProductoNavDto> items);
}
