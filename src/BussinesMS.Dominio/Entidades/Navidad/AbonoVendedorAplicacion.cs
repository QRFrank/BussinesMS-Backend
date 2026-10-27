using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Cómo se aplicó un abono a cada venta (para poder revertirlo)
public class AbonoVendedorAplicacion : EntidadBase
{
    public int AbonoVendedorId { get; set; }
    public int VentaId { get; set; }
    public decimal MontoCobrado { get; set; }
    public decimal MontoDescuento { get; set; }

    public AbonoVendedor? AbonoVendedor { get; set; }
    public VentaNav? Venta { get; set; }
}
