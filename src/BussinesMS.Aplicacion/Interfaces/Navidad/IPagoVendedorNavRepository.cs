using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IPagoVendedorNavRepository : IRepositorio<PagoVendedor>
{
    // CON tracking: detalles
    Task<PagoVendedor?> ObtenerParaEdicionAsync(int id);

    // Sin tracking: vendedor y detalles con producto
    Task<PagoVendedor?> ObtenerConDetallesAsync(int id);
}
