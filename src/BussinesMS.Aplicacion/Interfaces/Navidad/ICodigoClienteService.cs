using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ICodigoClienteService
{
    Task<PagedResultDto<CodigoClienteDto>> ObtenerTodosAsync(CodigoClienteFiltroDto query);
    Task<CodigoClienteDto?> ObtenerPorIdAsync(int id);
    Task<CodigoClienteDto> CrearAsync(CrearCodigoClienteDto dto);
    Task<CodigoClienteDto> ActualizarAsync(ActualizarCodigoClienteDto dto);
    Task EliminarAsync(int id);
}
