using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class CompraDetalle : EntidadBase
{
    public int CompraId { get; set; }
    public int ProductoId { get; set; }
    public int CantidadUnidades { get; set; }
    public decimal PrecioCompraUnidad { get; set; }

    public Compra? Compra { get; set; }
    public Producto? Producto { get; set; }
    public ICollection<CompraDistribucion> Distribuciones { get; set; } = new List<CompraDistribucion>();
}
