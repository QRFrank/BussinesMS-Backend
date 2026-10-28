using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IVentaClienteRutaNavRepository : IRepositorio<VentaClienteRuta>
{
    // CON tracking: detalles y cobros
    Task<VentaClienteRuta?> ObtenerParaEdicionAsync(int id);

    // Sin tracking: cliente, detalles con producto y cobros
    Task<VentaClienteRuta?> ObtenerConDetallesAsync(int id);
}
