using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ITrasladoNavRepository : IRepositorio<Traslado>
{
    // Sin tracking: detalles con producto
    Task<Traslado?> ObtenerConDetallesAsync(int id);
}
