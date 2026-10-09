using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class Pedido : EntidadBase
{
    public int TemporadaId { get; set; }
    public int ProveedorId { get; set; }
    public int? CodigoClienteId { get; set; }
    public DateTime Fecha { get; set; }
    public string? Observacion { get; set; }
    // Monto que informa el proveedor (por defecto, el calculado)
    public decimal MontoTotalProveedor { get; set; }

    public Temporada? Temporada { get; set; }
    public Proveedor? Proveedor { get; set; }
    public CodigoCliente? CodigoCliente { get; set; }
    public ICollection<PedidoDetalle> Detalles { get; set; } = new List<PedidoDetalle>();
}
