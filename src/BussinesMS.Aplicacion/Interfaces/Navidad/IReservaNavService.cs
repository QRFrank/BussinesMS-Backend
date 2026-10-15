using BussinesMS.Aplicacion.DTOs.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IReservaNavService
{
    // B7: cobertura por producto con reservas pendientes de entrega (solo lectura)
    Task<List<ReservaCoberturaNavDto>> ObtenerCoberturaAsync(ReservaCoberturaNavFiltroDto filtro);

    // Avisos de sobre-reservado (no bloquean) para los productos dados; los usan crear y editar venta
    Task<List<string>> ObtenerAdvertenciasAsync(int temporadaId, IEnumerable<int> productoIds);
}
