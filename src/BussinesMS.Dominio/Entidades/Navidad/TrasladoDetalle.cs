using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class TrasladoDetalle : EntidadBase
{
    public int TrasladoId { get; set; }
    public int ProductoId { get; set; }
    public int CantidadUnidades { get; set; }

    public Traslado? Traslado { get; set; }
    public Producto? Producto { get; set; }
}
