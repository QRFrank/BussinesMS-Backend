using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ICompraNavRepository : IRepositorio<Compra>
{
    // Sin tracking: proveedor, pago automático, detalles con producto y distribuciones
    Task<Compra?> ObtenerConDetallesAsync(int id);
}
