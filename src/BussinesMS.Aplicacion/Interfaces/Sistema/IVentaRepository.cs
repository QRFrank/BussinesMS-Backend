using BussinesMS.Dominio.Entidades.Sistema;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface IVentaRepository
{
    Task<List<Venta>> ObtenerTodosAsync();
    Task<Venta?> ObtenerPorIdAsync(int id);
    Task<Venta?> ObtenerConDetallesAsync(int id);
    Task<Venta> CrearAsync(Venta entidad);
    Task<Venta> CrearSinGuardarAsync(Venta entidad);
    Task<Venta> ActualizarAsync(Venta entidad);
    Task EliminarAsync(int id);
}
