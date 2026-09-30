using BussinesMS.Dominio.Entidades.Sistema;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface IMovimientoInventarioRepository
{
    IQueryable<MovimientoInventario> AsQueryable();
    Task<MovimientoInventario> CrearAsync(MovimientoInventario entidad);
    Task<MovimientoInventario> CrearSinGuardarAsync(MovimientoInventario entidad);
    Task<List<MovimientoInventario>> ObtenerTodosAsync();
    Task<List<MovimientoInventario>> ObtenerFiltradosAsync(
        int? loteId, int? varianteId, int? almacenOrigenId,
        int? almacenDestinoId, int? tipoMovimiento,
        DateTime? fechaDesde, DateTime? fechaHasta);
    Task<MovimientoInventario?> ObtenerPorIdAsync(int id);
}
