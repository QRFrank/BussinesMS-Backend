using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Cada detalle de recepción es un LOTE (su Id = LoteId)
public class RecepcionDetalle : EntidadBase
{
    public int RecepcionId { get; set; }
    public int ProductoId { get; set; }
    public int CantidadUnidades { get; set; }
    public decimal PrecioCompraUnidad { get; set; }

    public Recepcion? Recepcion { get; set; }
    public Producto? Producto { get; set; }
    public ICollection<RecepcionDistribucion> Distribuciones { get; set; } = new List<RecepcionDistribucion>();
}
