using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// A qué venta se aplicó el incobrable, para poder revertirlo
public class IncobrableNavAplicacion : EntidadBase
{
    public int IncobrableId { get; set; }
    public int VentaId { get; set; }
    public decimal Monto { get; set; }

    public IncobrableNav? Incobrable { get; set; }
    public VentaNav? Venta { get; set; }
}
