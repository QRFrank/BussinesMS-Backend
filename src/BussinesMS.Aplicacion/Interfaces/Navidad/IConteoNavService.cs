using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IConteoNavService
{
    Task<PagedResultDto<ConteoNavDto>> ObtenerTodosAsync(ConteoNavFiltroDto query);
    Task<ConteoNavDto?> ObtenerPorIdAsync(int id);
    Task<ConteoNavDto> CrearAsync(CrearConteoNavDto dto);
    Task<List<ConteoPlantillaNavDto>> ObtenerPlantillaAsync(int almacenId, int tipo);
    Task<ConteoEstadoNavDto> ObtenerEstadoAsync(int almacenId, DateTime? fecha);

    /// <summary>
    /// Para el módulo de ventas: si la tienda requiere apertura diaria y no la tiene de hoy (Bolivia) →
    /// 409 SIN_APERTURA. Si no requiere conteo no hace nada.
    /// </summary>
    Task VerificarAperturaParaVentaAsync(int almacenId);
}
