using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IInversorService
{
    Task<PagedResultDto<InversorDto>> ObtenerTodosAsync(GenericPaginationQueryDto query);
    Task<InversorDto?> ObtenerPorIdAsync(int id);
    Task<InversorDto> CrearAsync(CrearInversorDto dto);
    Task<InversorDto> ActualizarAsync(ActualizarInversorDto dto);
    Task EliminarAsync(int id);
}
