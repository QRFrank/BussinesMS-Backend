using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ICategoriaGastoNavService
{
    Task<PagedResultDto<CategoriaGastoNavDto>> ObtenerTodosAsync(GenericPaginationQueryDto query);
    Task<CategoriaGastoNavDto?> ObtenerPorIdAsync(int id);
    Task<CategoriaGastoNavDto> CrearAsync(CrearCategoriaGastoNavDto dto);
    Task<CategoriaGastoNavDto> ActualizarAsync(ActualizarCategoriaGastoNavDto dto);
    Task EliminarAsync(int id);
}
