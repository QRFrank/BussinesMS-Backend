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
    Task RegistrarSalidaAsync(int temporadaId, int almacenId, int productoId, int unidades,
        TipoMovimientoNav tipo, string referenciaTipo, int referenciaId, string? motivo = null);

    /// <summary>Stock actual del producto en el almacén (0 si no hay fila).</summary>
    Task<int> ObtenerDisponibleAsync(int almacenId, int productoId);

    /// <summary>
    /// Costo promedio ponderado por producto de la temporada: Σ(cantidad × precio) / Σ cantidad de todas las entradas
    /// no anuladas (líneas de recepción y de compra). Sin redondear. Productos sin entradas no aparecen.
    /// </summary>
    Task<Dictionary<int, decimal>> ObtenerCostosPromedioAsync(int temporadaId, IEnumerable<int>? productoIds = null);

    Task<List<StockNavDto>> ObtenerStockAsync(StockNavFiltroDto filtro);
}
