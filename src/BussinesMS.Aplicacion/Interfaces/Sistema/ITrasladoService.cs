using BussinesMS.Aplicacion.DTOs.Sistema;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface ITrasladoService
{
    Task<MovimientoInventarioDto> CrearPorLoteAsync(CrearTrasladoPorLoteDto dto);
}
