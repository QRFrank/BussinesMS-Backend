using BussinesMS.Aplicacion.DTOs.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IAbastecimientoNavService
{
    // B3: pedido vs recibido por (proveedor, código, producto)
    Task<List<FaltanteNavDto>> ObtenerFaltantesAsync(AbastecimientoNavFiltroDto filtro);

    // B5: recibido vs pagado por (proveedor, código)
    Task<List<DeudaProveedorNavDto>> ObtenerDeudasAsync(AbastecimientoNavFiltroDto filtro);
}
