using BussinesMS.Aplicacion.DTOs.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ICatalogoNavService
{
    Task<CopiaCatalogoResultadoDto> CopiarAsync(int destinoId, int temporadaOrigenId);
}
