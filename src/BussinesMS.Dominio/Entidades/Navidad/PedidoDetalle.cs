using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class PedidoDetalle : EntidadBase
{
    public int PedidoId { get; set; }
    public int ProductoId { get; set; }
    public int CantidadUnidades { get; set; }
    public decimal PrecioCompraUnidad { get; set; }

    public Pedido? Pedido { get; set; }
    public Producto? Producto { get; set; }
}
