using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IConteoInventarioNavRepository : IRepositorio<ConteoInventario>
{
    // Sin tracking: detalles con producto
    Task<ConteoInventario?> ObtenerConDetallesAsync(int id);
}
