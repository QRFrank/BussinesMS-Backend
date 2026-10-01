using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface IGastoOperativoService
{
    Task<PagedResultDto<GastoOperativoListDto>> ObtenerTodosAsync(GastoOperativoFiltroDto query);
    Task<GastoOperativoListDto?> ObtenerPorIdAsync(int id);
    Task<GastoOperativoListDto> CrearAsync(CrearGastoOperativoDto dto);
    Task<GastoOperativoListDto> ActualizarAsync(ActualizarGastoOperativoDto dto);
    Task AnularAsync(int id);
}
