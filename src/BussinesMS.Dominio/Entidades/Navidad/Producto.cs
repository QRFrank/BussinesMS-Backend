using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class Producto : EntidadBase
{
    public int TemporadaId { get; set; }
    public int ProveedorId { get; set; }
    public int CategoriaProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioCompraUnidad { get; set; }
    // Solo referencia, no bloquea nada
    public decimal PrecioCatalogo { get; set; }

    public Temporada? Temporada { get; set; }
    public Proveedor? Proveedor { get; set; }
    public CategoriaProductoNav? Categoria { get; set; }
    public ICollection<ProductoPresentacion> Presentaciones { get; set; } = new List<ProductoPresentacion>();
}
