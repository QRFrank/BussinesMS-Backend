using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface ICategoriaGastoService
{
    Task<PagedResultDto<CategoriaGastoDto>> ObtenerTodosAsync(GenericPaginationQueryDto query);
    Task<CategoriaGastoDto?> ObtenerPorIdAsync(int id);
    Task<CategoriaGastoDto> CrearAsync(CrearCategoriaGastoDto dto);
    Task<CategoriaGastoDto> ActualizarAsync(ActualizarCategoriaGastoDto dto);
    Task EliminarAsync(int id);
}
