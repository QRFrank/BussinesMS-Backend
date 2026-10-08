using BussinesMS.Aplicacion.DTOs.Auth;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Auth;

public interface IAlmacenService
{
    Task<PagedResultDto<AlmacenDto>> ObtenerTodosAsync(GenericPaginationQueryDto query, int sistemaId = 1);
    Task<AlmacenDto?> ObtenerPorIdAsync(int id);
    Task<AlmacenDto> CrearAsync(CrearAlmacenDto almacen);
    Task<AlmacenDto> ActualizarAsync(ActualizarAlmacenDto dto);
    Task EliminarAsync(int id);
}