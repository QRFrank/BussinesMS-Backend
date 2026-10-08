using BussinesMS.Aplicacion.DTOs.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IGastoNavService
{
    Task<GastoNavPagedResultDto> ObtenerTodosAsync(GastoNavFiltroDto query);
    Task<GastoNavDto?> ObtenerPorIdAsync(int id);
    Task<GastoNavDto> CrearAsync(CrearGastoNavDto dto);
    Task<GastoNavDto> ActualizarAsync(ActualizarGastoNavDto dto);
    Task EliminarAsync(int id);
}
