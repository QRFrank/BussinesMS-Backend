using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Sistema;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface IInventarioLoteAlmacenRepository : IRepositorio<InventarioLoteAlmacen>
{
    Task<InventarioLoteAlmacen?> ObtenerConDetallesAsync(int id);
    Task<List<InventarioLoteAlmacen>> ObtenerLotesFEFOAsync(int varianteId, int almacenId);
    Task<List<InventarioLoteAlmacen>> ObtenerLotesVencidosAsync(DateTime corte, int? almacenId = null, List<int>? varianteIds = null);
    Task<List<InventarioLoteAlmacen>> ObtenerPorLoteAsync(int loteId);
    Task<int> ObtenerStockDisponibleAsync(int varianteId, int almacenId);
    Task<int> ObtenerStockTotalVarianteAsync(int varianteId, int almacenId);
    Task<int> ObtenerStockTotalLoteAsync(int loteId);
    Task<InventarioLoteAlmacen?> ObtenerPorLoteYAlmacenAsync(int loteId, int almacenId);
    Task<InventarioLoteAlmacen> CrearSinGuardarAsync(InventarioLoteAlmacen entidad);
}
