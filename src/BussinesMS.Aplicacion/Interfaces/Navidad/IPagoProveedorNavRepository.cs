using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IPagoProveedorNavRepository : IRepositorio<PagoProveedor>
{
    // Sin tracking: proveedor y código
    Task<PagoProveedor?> ObtenerConDetallesAsync(int id);
}
