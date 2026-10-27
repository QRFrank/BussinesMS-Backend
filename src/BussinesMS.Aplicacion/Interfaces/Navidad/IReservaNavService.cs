using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

// Reservas (replanteo 2026-10-15): se guardan en VentaNav con Tipo = Reserva, sin tienda, sin descuento de stock al crear
// y con bloqueo de stock por almacén (ReservaBloqueo). Cobros, anular entrega y anular reutilizan los de IVentaNavService.
public interface IReservaNavService
{
    // Reservas (Tipo = Reserva forzado) con los mismos filtros que las ventas
    Task<PagedResultDto<VentaNavDto>> ObtenerTodosAsync(VentaNavFiltroDto query);
    // Null si no existe o no es una reserva
    Task<VentaNavDto?> ObtenerPorIdAsync(int id);

    // Productos activos con stock total, por llegar libre y stock libre por almacén. reservaId: no cuenta la parte sin bloqueo de esa reserva (al editarla)
    Task<List<ReservaProductoNavDto>> ObtenerProductosAsync(int? reservaId);

    Task<VentaNavDto> CrearAsync(CrearReservaNavDto dto);
    Task<VentaNavDto> EditarAsync(int id, EditarReservaNavDto dto);

    Task<VentaNavDto> RegistrarEntregaAsync(int ventaId, RegistrarEntregaVentaDto dto);

    // Solo admin: libera todo o una parte de un bloqueo
    Task<VentaNavDto> LiberarBloqueoAsync(int ventaId, LiberarBloqueoReservaDto dto);
}
