using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IClienteNavService
{
    Task<PagedResultDto<ClienteNavDto>> ObtenerTodosAsync(GenericPaginationQueryDto query);
    Task<ClienteNavDto?> ObtenerPorIdAsync(int id);
    Task<ClienteNavDto> CrearAsync(CrearClienteNavDto dto);
    Task<ClienteNavDto> ActualizarAsync(ActualizarClienteNavDto dto);
    Task EliminarAsync(int id);
}
