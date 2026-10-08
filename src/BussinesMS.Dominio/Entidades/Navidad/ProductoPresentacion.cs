using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class ProductoPresentacion : EntidadBase
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Unidades { get; set; }
    public decimal PrecioUnitario { get; set; }
    public bool EsPrincipal { get; set; }

    public Producto? Producto { get; set; }
}
