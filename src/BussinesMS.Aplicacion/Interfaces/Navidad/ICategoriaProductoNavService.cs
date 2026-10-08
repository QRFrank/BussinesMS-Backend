using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ICategoriaProductoNavService
{
    Task<PagedResultDto<CategoriaProductoNavDto>> ObtenerTodosAsync(GenericPaginationQueryDto query);
    Task<CategoriaProductoNavDto?> ObtenerPorIdAsync(int id);
    Task<CategoriaProductoNavDto> CrearAsync(CrearCategoriaProductoNavDto dto);
    Task<CategoriaProductoNavDto> ActualizarAsync(ActualizarCategoriaProductoNavDto dto);
    Task EliminarAsync(int id);
}
