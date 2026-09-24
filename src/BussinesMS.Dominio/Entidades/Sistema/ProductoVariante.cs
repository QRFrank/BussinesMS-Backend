using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Sistema;

public class ProductoVariante : EntidadBase
{
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public string? CodigoBarras { get; set; }

    public int SaborId { get; set; }
    public DescripcionSabor? Sabor { get; set; }

    public int? CantidadCaja { get; set; }

    public int TamanioId { get; set; }
    public DescripcionTamanio? Tamanio { get; set; }

    public decimal PrecioVentaUnitario { get; set; }
    public decimal PrecioVentaMayoreo { get; set; }
    public decimal PrecioCompra { get; set; }
    public string? CodigoAlmacen { get; set; }

    public ICollection<ProductoPresentacion> Presentaciones { get; set; } = [];
}
