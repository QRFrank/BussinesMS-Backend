using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IPedidoNavRepository : IRepositorio<Pedido>
{
    // Sin tracking: proveedor, código y detalles con producto
    Task<Pedido?> ObtenerConDetallesAsync(int id);

    // Borra físicamente los detalles del pedido y crea los nuevos (corre dentro de la transacción del llamador)
    Task ReemplazarDetallesAsync(int pedidoId, IEnumerable<PedidoDetalle> nuevos);
}
