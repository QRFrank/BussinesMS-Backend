using BussinesMS.Aplicacion.DTOs.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

// Cobertura de reservas. Desde 2026-11-08 la cobertura es global por producto:
// Libre = Σ stock en almacenes (StockAlmacen de la temporada) + por llegar (faltantes de pedidos) − Σ pendiente de entregar de las reservas vigentes.
// El bloqueo por almacén (ReservaBloqueo) queda como opción manual.
public interface IReservaCoberturaNavService
{
    // GET Reservas/cobertura: por producto con reservas pendientes de entrega (solo lectura)
    Task<List<ReservaCoberturaNavDto>> ObtenerCoberturaAsync(ReservaCoberturaNavFiltroDto filtro);

    // Por producto (sin contar la reserva excluirVentaId): por llegar, por llegar libre (legado: por llegar − Σ partes sin bloqueo),
    // reservado pendiente y Libre (stock + por llegar − reservado pendiente). Productos sin stock, pedido ni reservas no aparecen (todo 0).
    Task<Dictionary<int, (int PorLlegar, int PorLlegarLibre, int Reservado, int Libre)>> ObtenerPorLlegarAsync(
        int temporadaId, IEnumerable<int>? productoIds = null, int? excluirVentaId = null);

    // Reservas (crear/editar): 409 SIN_COBERTURA si el pendiente de alguna línea supera el Libre del producto
    // (Libre calculado sin la reserva excluirVentaId)
    Task ValidarCoberturaAsync(int temporadaId, IReadOnlyList<(int ProductoId, string ProductoNombre, int Pendiente)> lineas, int? excluirVentaId = null);

    // Ventas directas y cargas a ruta: 409 RESERVAS_COMPROMETIDAS si sacar esas unidades deja Libre < 0 en algún producto con reservas pendientes.
    // accion: verbo para el mensaje ("vender", "sacar a ruta"). Llamar dentro de la transacción de la operación, antes de descontar el stock.
    Task ValidarLibreParaSalidaAsync(int temporadaId, IReadOnlyList<(int ProductoId, string ProductoNombre, int Unidades)> lineas,
        string accion, int? excluirVentaId = null);

    // Sin uso desde 2026-11-08 (la recepción ya no bloquea automáticamente). Se conserva por si se reactiva.
    Task BloquearPorRecepcionAsync(int temporadaId, int recepcionId, IEnumerable<(int AlmacenId, int ProductoId, int Unidades)> recibido);

    // Al anular una recepción (dentro de su transacción): elimina los bloqueos vigentes que generó (bloqueos viejos)
    Task QuitarBloqueosDeRecepcionAsync(int recepcionId);
}
