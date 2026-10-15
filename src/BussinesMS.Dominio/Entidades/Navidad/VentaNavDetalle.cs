using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Una fila por producto y venta. Quitar una línea la deja en Unidades = 0 (se conserva por el historial de entregas)
public class VentaNavDetalle : EntidadBase
{
    public int VentaId { get; set; }
    public int ProductoId { get; set; }
    // Siempre en unidades
    public int Unidades { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public int UnidadesEntregadas { get; set; }
    public EstadoEntregaVentaNav EstadoEntrega { get; set; }

    public VentaNav? Venta { get; set; }
    public Producto? Producto { get; set; }
}
