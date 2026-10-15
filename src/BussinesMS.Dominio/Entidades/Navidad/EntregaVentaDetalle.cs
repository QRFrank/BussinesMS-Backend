using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Invariante: Σ Unidades de las entregas no anuladas de una línea = VentaNavDetalle.UnidadesEntregadas
public class EntregaVentaDetalle : EntidadBase
{
    public int EntregaId { get; set; }
    public int VentaDetalleId { get; set; }
    public int ProductoId { get; set; }
    public int Unidades { get; set; }

    public EntregaVenta? Entrega { get; set; }
    public VentaNavDetalle? VentaDetalle { get; set; }
    public Producto? Producto { get; set; }
}
