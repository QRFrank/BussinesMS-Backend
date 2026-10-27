using BussinesMS.Aplicacion.DTOs.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

// Cobertura de reservas (replanteo 2026-10-15): cada línea se cubre con bloqueo de stock por almacén (ReservaBloqueo)
// y/o con lo que falta llegar de los pedidos ("por llegar libre").
public interface IReservaCoberturaNavService
{
    // GET Reservas/cobertura: por producto con reservas pendientes de entrega (solo lectura)
    Task<List<ReservaCoberturaNavDto>> ObtenerCoberturaAsync(ReservaCoberturaNavFiltroDto filtro);

    // Por llegar y por llegar libre por producto (por llegar − Σ partes sin bloqueo de las reservas vigentes, sin contar excluirVentaId).
    // Productos sin pedido ni reservas no aparecen (equivale a 0 / 0).
    Task<Dictionary<int, (int PorLlegar, int PorLlegarLibre)>> ObtenerPorLlegarAsync(
        int temporadaId, IEnumerable<int>? productoIds = null, int? excluirVentaId = null);

    // 409 SIN_COBERTURA_PEDIDO si la parte sin bloqueo de alguna línea supera el por llegar libre (sin contar la reserva excluirVentaId)
    Task ValidarCoberturaAsync(int temporadaId, IReadOnlyList<(int ProductoId, string ProductoNombre, int SinBloqueo)> lineas, int? excluirVentaId = null);

    // RB5: tras registrar una recepción (dentro de su transacción), bloquea en cada almacén donde llegó mercadería las reservas
    // vigentes con parte sin bloqueo del producto, de la más antigua a la más nueva
    Task BloquearPorRecepcionAsync(int temporadaId, int recepcionId, IEnumerable<(int AlmacenId, int ProductoId, int Unidades)> recibido);

    // Al anular una recepción (dentro de su transacción): elimina los bloqueos vigentes que generó
    Task QuitarBloqueosDeRecepcionAsync(int recepcionId);
}
