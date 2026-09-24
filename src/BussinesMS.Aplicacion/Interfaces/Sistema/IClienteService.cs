using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface IClienteService
{
    Task<PagedResultDto<ClienteDto>> ObtenerTodosAsync(GenericPaginationQueryDto query);
    Task<ClienteDto?> ObtenerPorIdAsync(int id);
    Task<ClienteDto?> ObtenerPorNumeroCarnetAsync(string numeroCarnet);
    Task<(ClienteDto Entidad, bool FueReactivada)> CrearAsync(CrearClienteDto dto);
    Task<ClienteDto> ActualizarAsync(ActualizarClienteDto dto);
    Task EliminarAsync(int id);
}
