using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class Producto : EntidadBase
{
    public int TemporadaId { get; set; }
    public int ProveedorId { get; set; }
    public int CategoriaProductoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    // Alias opcional; único por temporada entre activos
    public string? Nombre { get; set; }
    public decimal PrecioCompraUnidad { get; set; }
    // Solo referencia, no bloquea nada
    public decimal PrecioCatalogo { get; set; }
    // Empaque opcional: van juntos; UnidadesPorEmpaque > 1
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    // Color de la tarjeta en pantalla: hex "#RRGGBB" o null
    public string? Color { get; set; }

    public Temporada? Temporada { get; set; }
    public Proveedor? Proveedor { get; set; }
    public CategoriaProductoNav? Categoria { get; set; }
}
