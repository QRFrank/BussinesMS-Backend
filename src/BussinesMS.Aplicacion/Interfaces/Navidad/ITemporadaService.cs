using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ITemporadaService
{
    Task<PagedResultDto<TemporadaDto>> ObtenerTodosAsync(GenericPaginationQueryDto query);
    Task<TemporadaDto?> ObtenerPorIdAsync(int id);
    Task<TemporadaDto> ObtenerActualAsync();
    Task<TemporadaDto> CrearAsync(CrearTemporadaDto dto);
    Task<TemporadaDto> ActualizarAsync(ActualizarTemporadaDto dto);
    Task<TemporadaDto> CerrarAsync(int id);
}
