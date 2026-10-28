using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Línea de una venta personal del vendedor de ruta; el precio es el que puso el vendedor (libre, >= 0)
public class VentaClienteRutaDetalle : EntidadBase
{
    public int VentaClienteRutaId { get; set; }
    public int ProductoId { get; set; }
    public int Unidades { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public VentaClienteRuta? VentaClienteRuta { get; set; }
    public Producto? Producto { get; set; }
}
