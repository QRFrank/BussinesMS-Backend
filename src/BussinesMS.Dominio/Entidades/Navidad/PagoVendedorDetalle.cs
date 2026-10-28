using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Línea de una comisión por producto: unidades vendidas, comisión por unidad escrita en ese pago y subtotal
public class PagoVendedorDetalle : EntidadBase
{
    public int PagoVendedorId { get; set; }
    public int ProductoId { get; set; }
    public int Unidades { get; set; }
    public decimal ComisionUnidad { get; set; }
    public decimal Subtotal { get; set; }

    public PagoVendedor? PagoVendedor { get; set; }
    public Producto? Producto { get; set; }
}
