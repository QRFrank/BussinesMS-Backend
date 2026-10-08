using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IProductoPresentacionNavRepository : IRepositorio<ProductoPresentacion>
{
    Task<List<ProductoPresentacion>> ObtenerActivasPorProductoAsync(int productoId);
}
