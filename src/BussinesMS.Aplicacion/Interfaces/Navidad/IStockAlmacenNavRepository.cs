using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IStockAlmacenNavRepository : IRepositorio<StockAlmacen>
{
    // Con tracking, sin navegaciones
    Task<StockAlmacen?> ObtenerAsync(int productoId, int almacenId);
}
