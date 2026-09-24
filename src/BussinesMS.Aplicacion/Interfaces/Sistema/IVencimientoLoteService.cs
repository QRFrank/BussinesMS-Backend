using BussinesMS.Dominio.Entidades.Sistema;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface IVencimientoLoteService
{
    Task<List<InventarioLoteAlmacen>> ObtenerLotesVencidosAsync(DateTime corte, int? almacenId = null, List<int>? varianteIds = null);
    Task<int> MarcarComoVencidosAsync(IEnumerable<int> loteIds);
    Task<int> ProcesarVencimientosPendientesAsync(int? almacenId = null, List<int>? varianteIds = null);
}
