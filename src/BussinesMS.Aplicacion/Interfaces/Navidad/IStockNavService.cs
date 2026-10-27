using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

// Stock por producto × almacén (Ajuste 2: sin lotes ni FIFO; costo promedio ponderado).
// Los métodos que escriben NO abren transacción: corren dentro de la del llamador (INavidadUnitOfWork).
public interface IStockNavService
{
    /// <summary>
    /// Entrada (+): suma al StockAlmacen del producto en el almacén (lo crea si no existe) y registra un MovimientoNav (+).
    /// </summary>
    Task RegistrarEntradaAsync(int temporadaId, int almacenId, int productoId, int unidades,
        TipoMovimientoNav tipo, string referenciaTipo, int referenciaId, string? motivo = null);

    /// <summary>
    /// Salida (−) reutilizable (ventas, traslados, ruta, anulaciones): descuenta del StockAlmacen y registra un MovimientoNav (−).
    /// Stock insuficiente → 409 STOCK_INSUFICIENTE (el stock nunca queda negativo).
    /// </summary>
    /// <remarks>
    /// Módulo ventas: con <paramref name="respetarBloqueo"/> (por defecto sí) las unidades apartadas por reservas con bloqueo no se pueden sacar:
    /// 409 STOCK_BLOQUEADO. <paramref name="ventaIdPropia"/> excluye del bloqueo a la reserva que está entregando. El ajuste de conteo pasa false.
    /// </remarks>
    Task RegistrarSalidaAsync(int temporadaId, int almacenId, int productoId, int unidades,
        TipoMovimientoNav tipo, string referenciaTipo, int referenciaId, string? motivo = null,
        bool respetarBloqueo = true, int? ventaIdPropia = null);

    /// <summary>Stock actual del producto en el almacén (0 si no hay fila).</summary>
    Task<int> ObtenerDisponibleAsync(int almacenId, int productoId);

    /// <summary>
    /// Unidades bloqueadas del producto en el almacén: Σ de los ReservaBloqueo vigentes de reservas no anuladas en ese almacén. <paramref name="excluirVentaId"/> deja afuera a una reserva (la propia).
    /// </summary>
    Task<int> ObtenerBloqueadoAsync(int almacenId, int productoId, int? excluirVentaId = null);

    /// <summary>Bloqueado por (almacén, producto) de la temporada (opcionalmente de un solo almacén). Solo entradas con bloqueado &gt; 0.</summary>
    Task<Dictionary<(int AlmacenId, int ProductoId), int>> ObtenerBloqueadosAsync(int temporadaId, int? almacenId = null);

    /// <summary>
    /// Verifica que "stock − bloqueado (sin la venta propia)" alcance para <paramref name="unidades"/>:
    /// 409 STOCK_INSUFICIENTE si el stock no alcanza, 409 STOCK_BLOQUEADO si alcanza pero hay unidades apartadas por reservas.
    /// </summary>
    Task VerificarLibreAsync(int almacenId, int productoId, int unidades, int? ventaIdPropia = null);

    /// <summary>
    /// Costo promedio ponderado por producto de la temporada: Σ(cantidad × precio) / Σ cantidad de todas las entradas
    /// no anuladas (líneas de recepción y de compra). Sin redondear. Productos sin entradas no aparecen.
    /// </summary>
    Task<Dictionary<int, decimal>> ObtenerCostosPromedioAsync(int temporadaId, IEnumerable<int>? productoIds = null);

    Task<List<StockNavDto>> ObtenerStockAsync(StockNavFiltroDto filtro);
}
