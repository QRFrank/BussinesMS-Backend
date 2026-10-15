using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Despacho de una venta: lo único que mueve stock de una venta de tienda
public class EntregaVenta : EntidadBase
{
    public int VentaId { get; set; }
    // Cualquier almacén navideño. Sin FK: Almacen vive en AuthDB
    public int AlmacenId { get; set; }
    public DateTime Fecha { get; set; }
    // Sin FK: Usuario vive en AuthDB
    public int UsuarioId { get; set; }
    public bool Anulada { get; set; }

    public VentaNav? Venta { get; set; }
    public ICollection<EntregaVentaDetalle> Detalles { get; set; } = new List<EntregaVentaDetalle>();
}
