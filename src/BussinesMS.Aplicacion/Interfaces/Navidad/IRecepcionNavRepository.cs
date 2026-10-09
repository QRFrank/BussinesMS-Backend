using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IRecepcionNavRepository : IRepositorio<Recepcion>
{
    // Sin tracking: proveedor, código, detalles con producto y distribuciones
    Task<Recepcion?> ObtenerConDetallesAsync(int id);
}
